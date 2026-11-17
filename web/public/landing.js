import('/ui.js').then(({polish})=>polish()).catch(error=>console.error('UI enhancement failed',error));
import('/home.js').catch(error=>{document.documentElement.classList.remove('fx');console.error('Homepage motion failed',error);});
