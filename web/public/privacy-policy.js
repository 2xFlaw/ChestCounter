import '/privacy-i18n.js';
import { sections as es } from '/privacy-es-data.js';
import * as de from '/privacy-de.js';
const translations={es:{sections:es,intro:'Este aviso explica cómo Chest Counter, operado bajo el nombre de 2xFlaw, trata la información cuando utilizas chestcounter.io y las funciones en línea opcionales de la aplicación de Windows. Para preguntas o solicitudes sobre privacidad, contacta con <a href="mailto:flxwlesslybusiness@gmail.com">flxwlesslybusiness@gmail.com</a>. Chest Counter es un proyecto independiente, no un servicio de Epic Games.',updated:'Última actualización: 18 de noviembre de 2026'},de};
const main=document.querySelector('main');
const sections=[...main.querySelectorAll(':scope > section')];
const intro=main.querySelector(':scope > p:not(.note)');
const updated=main.querySelector(':scope > p.note');
const english={sections:sections.map(s=>s.innerHTML),intro:intro.innerHTML,updated:updated.textContent};
const storage=document.getElementById('language-storage');
let current=null;
function paint(){
 const code=document.documentElement.lang.toLowerCase().split('-')[0];
 const key=Object.hasOwn(translations,code)?code:'en';
 if(current===key)return;
 const data=key==='en'?english:translations[key];
 if(sections.length!==data.sections.length){console.error('Privacy policy section count changed; translation not applied');return;}
 current=key;
 sections.forEach((section,i)=>{
  section.innerHTML=data.sections[i];
  if(i===4&&storage){const slot=section.querySelector('[data-language-slot],#language-storage');if(slot)slot.replaceWith(storage);}
 });
 intro.innerHTML=data.intro;
 updated.textContent=data.updated;
}
paint();
new MutationObserver(paint).observe(document.documentElement,{attributes:true,attributeFilter:['lang']});
