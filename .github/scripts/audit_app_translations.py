import collections
import json
import pathlib
import re
import sys

root = pathlib.Path(__file__).resolve().parents[2]
app = root / 'src' / 'ChestCounter'
out = root / 'audit'
out.mkdir(exist_ok=True)
string = r'"(?:\\.|[^"\\])*"'
values = rf'(?P<values>(?:\s*{string}\s*,?)+)'
rows = re.compile(rf'\[\s*(?P<key>{string}|[A-Za-z_]\w*)\s*\]\s*=\s*new\[\]\s*\{{{values}\}}', re.S)
slots = re.compile(r'(?<!\{)\{(\d+)(?:,[^}:]+)?(?::[^}]+)?\}(?!\})')
errors = []
checked = 0
files = sorted(set(app.glob('*Text.cs')) | {app / 'WeeklyChanceMore.cs'})

def decode(value):
    return json.loads(value)

def placeholders(value):
    return collections.Counter(slots.findall(value))

for path in files:
    if not path.exists():
        errors.append(f'Missing expected translation source: {path.name}')
        continue
    source = path.read_text(encoding='utf-8-sig')
    constants = {}
    for match in re.finditer(rf'const\s+string\s+(\w+)\s*=\s*({string})\s*;', source):
        constants[match[1]] = decode(match[2])
    for match in rows.finditer(source):
        key_token = match['key']
        key = decode(key_token) if key_token.startswith(chr(34)) else constants.get(key_token)
        if key is None:
            errors.append(f'{path.name}: unresolved key {key_token}')
            continue
        translated = [decode(token.group()) for token in re.finditer(string, match['values'])]
        checked += 1
        if len(translated) != 9:
            errors.append(f'{path.name}: expected 9 translations, found {len(translated)}: {key[:100]}')
        for index, text in enumerate(translated):
            if not text.strip() or placeholders(text) != placeholders(key):
                errors.append(f'{path.name}: blank text or placeholder mismatch at column {index}: {key[:100]}')
    if path.name == 'WeeklyChanceMore.cs':
        note = re.search(rf'private\s+static\s+readonly\s+string\[\]\s+Notes\s*=\s*\{{{values}\}}', source, re.S)
        if not note or len(list(re.finditer(string, note['values']))) != 9:
            errors.append('WeeklyChanceMore.cs: expected 9 explanation-note translations')

if checked == 0:
    errors.append('No translation rows were detected; the audit parser must be updated.')

rendered = list((app / 'obj').rglob('MainForm241.g.cs')) + list((app / 'obj').rglob('WeeklyChances241.g.cs')) + list((app / 'obj').rglob('RewardsForm241.g.cs'))
if not rendered:
    errors.append('No generated UI sources found; a successful Windows build is required for the text inventory.')

candidates = []
assignment = re.compile(rf'\b(?:Text|HeaderText|PlaceholderText)\s*=\s*({string})')
drawing = re.compile(rf'ChanceText\.Draw\(\s*g\s*,\s*({string})')
for path in rendered + [app / 'PersonalTools.cs', app / 'TreasureHourStrip.cs']:
    source = path.read_text(encoding='utf-8-sig')
    for pattern in (assignment, drawing):
        for match in pattern.finditer(source):
            text = decode(match[1])
            if re.search(r'[A-Za-z]{3}', text):
                line = source.count('\n', 0, match.start()) + 1
                candidates.append(f'{path.relative_to(root)}:{line}: {text}')

report = [f'Translation rows checked: {checked}', f'Validation errors: {len(errors)}', *errors, '', 'Remaining English candidates (review required; branding and technical text may be intentional):', *sorted(set(candidates)), '', 'Scope: ordinary-string dictionary rows in supplemental text helpers and literal UI assignments/draw calls.', 'Not proof of complete localization: interpolated fragments, computed strings, inherited controls and embedded HTML require separate review.', 'This audit does not test UI layout, gameplay, probability correctness or persistence across resets.']
(out / 'translation-audit.txt').write_text('\n'.join(report), encoding='utf-8')
print(f'Checked {checked} translation rows; {len(errors)} validation errors; {len(set(candidates))} English candidates.')
sys.exit(1 if errors else 0)
