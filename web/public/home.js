import { motion } from '/fx.js';

const $ = (s, r = document) => r.querySelector(s);
const $$ = (s, r = document) => [...r.querySelectorAll(s)];

function split(el) {
  el.setAttribute('aria-label', el.textContent.replace(/\s+/g, ' ').trim());
  let n = 0;
  const walk = (node) => {
    [...node.childNodes].forEach((c) => {
      if (c.nodeType === 3) {
        const frag = document.createDocumentFragment();
        c.textContent.split(/(\s+)/).forEach((part) => {
          if (!part) return;
          if (/^\s+$/.test(part)) { frag.append(' '); return; }
          const w = document.createElement('b');
          w.className = 'w';
          w.setAttribute('aria-hidden', 'true');
          const i = document.createElement('i');
          i.textContent = part;
          i.style.setProperty('--i', n++);
          w.append(i);
          frag.append(w);
        });
        c.replaceWith(frag);
      } else if (c.nodeType === 1 && c.tagName !== 'BR') {
        walk(c);
      }
    });
  };
  walk(el);
}

function marquee() {
  const strip = $('.ticker[data-marquee]');
  const track = strip && $('.track', strip);
  if (!track) return;
  const one = track.innerHTML;
  let half = one;
  track.innerHTML = half;
  while (track.scrollWidth < innerWidth * 1.2 && half.length < 40000) { half += one; track.innerHTML = half; }
  track.innerHTML = half + half;
  track.style.setProperty('--mq', Math.max(30, Math.round(track.scrollWidth / 2 / 60)) + 's');
}

function particles(canvas) {
  const gl = canvas.getContext('webgl', { alpha: true, antialias: false, premultipliedAlpha: true });
  if (!gl) return;
  const vs = `attribute vec3 aPos;attribute float aSeed;uniform float uTime,uRX,uRY,uAspect,uDpr,uK;varying float vA;varying float vH;
void main(){vec3 p=aPos*(1.0+0.05*sin(uTime*.8+aSeed*40.0))*uK;float cy=cos(uRY+uTime*.12),sy=sin(uRY+uTime*.12);p=vec3(cy*p.x+sy*p.z,p.y,-sy*p.x+cy*p.z);float cx=cos(uRX),sx=sin(uRX);p=vec3(p.x,cx*p.y-sx*p.z,sx*p.y+cx*p.z);float z=p.z+3.6;gl_Position=vec4(p.x*2.1/z/uAspect,p.y*2.1/z,0.0,1.0);gl_PointSize=(2.0+aSeed*5.0)*uDpr*(3.6/z);vA=clamp(0.25+(1.0-(z-2.6)/2.0)*0.9,0.15,1.0);vH=aSeed;}`;
  const fs = `precision mediump float;varying float vA;varying float vH;
void main(){float d=length(gl_PointCoord-vec2(.5));if(d>.5)discard;float a=smoothstep(.5,.0,d);vec3 c=mix(vec3(1.0,.40,.70),vec3(1.0,.72,.30),step(.78,vH));gl_FragColor=vec4(c*a*vA*1.4,a*vA*.9);}`;
  const compile = (type, src) => {
    const s = gl.createShader(type);
    gl.shaderSource(s, src);
    gl.compileShader(s);
    return gl.getShaderParameter(s, gl.COMPILE_STATUS) ? s : null;
  };
  const v = compile(gl.VERTEX_SHADER, vs), f = compile(gl.FRAGMENT_SHADER, fs);
  if (!v || !f) return;
  const prog = gl.createProgram();
  gl.attachShader(prog, v);
  gl.attachShader(prog, f);
  gl.linkProgram(prog);
  if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) return;
  gl.useProgram(prog);

  const N = innerWidth < 700 ? 500 : 1100;
  const data = new Float32Array(N * 4);
  for (let i = 0; i < N; i++) {
    let x, y, z;
    if (i % 4 === 0) {
      const a = Math.random() * Math.PI * 2, r = 1.55 + (Math.random() - 0.5) * 0.35;
      x = Math.cos(a) * r; z = Math.sin(a) * r; y = (Math.random() - 0.5) * 0.12;
    } else {
      const u = Math.random() * 2 - 1, t = Math.random() * Math.PI * 2, s = Math.sqrt(1 - u * u), r = 0.95 + Math.random() * 0.25;
      x = r * s * Math.cos(t); y = r * u; z = r * s * Math.sin(t);
    }
    data.set([x, y, z, Math.random()], i * 4);
  }
  const buf = gl.createBuffer();
  gl.bindBuffer(gl.ARRAY_BUFFER, buf);
  gl.bufferData(gl.ARRAY_BUFFER, data, gl.STATIC_DRAW);
  const aPos = gl.getAttribLocation(prog, 'aPos'), aSeed = gl.getAttribLocation(prog, 'aSeed');
  gl.enableVertexAttribArray(aPos);
  gl.vertexAttribPointer(aPos, 3, gl.FLOAT, false, 16, 0);
  gl.enableVertexAttribArray(aSeed);
  gl.vertexAttribPointer(aSeed, 1, gl.FLOAT, false, 16, 12);
  const U = (n) => gl.getUniformLocation(prog, n);
  const uTime = U('uTime'), uRX = U('uRX'), uRY = U('uRY'), uAspect = U('uAspect'), uDpr = U('uDpr'), uK = U('uK');
  gl.enable(gl.BLEND);
  gl.blendFunc(gl.SRC_ALPHA, gl.ONE);
  gl.clearColor(0, 0, 0, 0);

  let dpr = 1;
  const resize = () => {
    dpr = Math.min(devicePixelRatio || 1, 2);
    canvas.width = Math.max(2, Math.round(canvas.clientWidth * dpr));
    canvas.height = Math.max(2, Math.round(canvas.clientHeight * dpr));
    gl.viewport(0, 0, canvas.width, canvas.height);
  };
  resize();
  addEventListener('resize', resize);

  let tx = 0, ty = 0, rx = -0.3, ry = 0, visible = true, running = false;
  const hero = $('.hero.scene');
  hero.addEventListener('pointermove', (e) => {
    const r = hero.getBoundingClientRect();
    tx = ((e.clientX - r.left) / r.width - 0.5) * 2;
    ty = ((e.clientY - r.top) / r.height - 0.5) * 2;
  }, { passive: true });
  hero.addEventListener('pointerleave', () => { tx = 0; ty = 0; });

  const t0 = performance.now();
  const draw = (now) => {
    if (!visible || document.hidden) { running = false; return; }
    rx += ((-0.3 + ty * 0.3) - rx) * 0.05;
    ry += ((tx * 0.6) - ry) * 0.05;
    const h = document.documentElement.scrollHeight - innerHeight;
    const k = 1 - Math.min(1, h > 0 ? scrollY / h : 0) * 0.25;
    gl.clear(gl.COLOR_BUFFER_BIT);
    gl.uniform1f(uTime, (now - t0) / 1000);
    gl.uniform1f(uRX, rx);
    gl.uniform1f(uRY, ry);
    gl.uniform1f(uAspect, canvas.width / canvas.height);
    gl.uniform1f(uDpr, dpr);
    gl.uniform1f(uK, k);
    gl.drawArrays(gl.POINTS, 0, N);
    requestAnimationFrame(draw);
  };
  const start = () => { if (!running) { running = true; requestAnimationFrame(draw); } };
  new IntersectionObserver((es) => { visible = es[0].isIntersecting; if (visible) start(); }).observe(canvas);
  document.addEventListener('visibilitychange', () => { if (!document.hidden) start(); });
  start();
}

function init() {
  const hero = $('.hero.scene');
  if (!hero) return;
  $$('[data-split]').forEach(split);
  marquee();
  const gl = $('canvas.gl');
  if (gl) { try { particles(gl); } catch (e) { gl.remove(); } }

  const go = () => hero.classList.add('go');
  const ready = document.fonts && document.fonts.ready ? document.fonts.ready : Promise.resolve();
  ready.then(() => requestAnimationFrame(() => setTimeout(go, 80)));
  setTimeout(go, 1500);

  const clean = (c) => setTimeout(() => $$('[data-r]', c).forEach((n) => n.removeAttribute('data-r')), 4200);
  clean(hero);
  const io = new IntersectionObserver((es) => es.forEach((e) => {
    if (!e.isIntersecting) return;
    e.target.classList.add('go');
    clean(e.target);
    io.unobserve(e.target);
  }), { threshold: 0.22 });
  $$('.feature, .finish').forEach((s) => io.observe(s));

  const items = $$('[data-speed], [data-depth]').map((el) => ({ el, speed: Number(el.dataset.speed) || 0, depth: Number(el.dataset.depth) || 0, host: el.closest('section') || hero }));
  const features = $$('.feature');
  const copy = $('.copy', hero);
  let mx = 0, my = 0, cx = 0, cy = 0, raf = 0;
  const wake = () => { if (!raf) raf = requestAnimationFrame(frame); };
  hero.addEventListener('pointermove', (e) => {
    const r = hero.getBoundingClientRect();
    mx = ((e.clientX - r.left) / r.width - 0.5) * 2;
    my = ((e.clientY - r.top) / r.height - 0.5) * 2;
    wake();
  }, { passive: true });
  hero.addEventListener('pointerleave', () => { mx = 0; my = 0; wake(); });
  addEventListener('scroll', wake, { passive: true });
  addEventListener('resize', wake);

  function frame() {
    raf = 0;
    cx += (mx - cx) * 0.08;
    cy += (my - cy) * 0.08;
    const vh = innerHeight;
    const rects = new Map();
    items.forEach((it) => { if (!rects.has(it.host)) rects.set(it.host, it.host.getBoundingClientRect()); });
    const frs = features.map((f) => f.getBoundingClientRect());
    items.forEach((it) => {
      const r = rects.get(it.host);
      if (r.bottom < -200 || r.top > vh + 200) return;
      const rel = (r.top + r.height / 2 - vh / 2) / vh;
      it.el.style.translate = `${(cx * it.depth).toFixed(1)}px ${(rel * it.speed + cy * it.depth * 0.6).toFixed(1)}px`;
    });
    const p = Math.min(1, Math.max(0, scrollY / (vh * 0.7)));
    if (copy) {
      copy.style.translate = `0 ${(-p * 70).toFixed(1)}px`;
      copy.style.opacity = (1 - p * 1.15).toFixed(3);
    }
    features.forEach((f, i) => {
      const r = frs[i];
      const rel = Math.max(-1, Math.min(1, (r.top + r.height / 2 - vh / 2) / vh));
      f.style.setProperty('--tilt', (rel * -16).toFixed(2));
      f.style.setProperty('--sc', (1 - Math.abs(rel) * 0.1).toFixed(3));
    });
    if (Math.abs(mx - cx) > 0.001 || Math.abs(my - cy) > 0.001) raf = requestAnimationFrame(frame);
  }
  frame();
}

if (motion) {
  try { init(); } catch (e) { document.documentElement.classList.remove('fx'); console.error('Homepage motion failed', e); }
}
