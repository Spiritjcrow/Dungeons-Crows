import {createDreamachine,stepDreamachine,actDreamachine,observe,inferDirector,exportDreamachine,resetMemory,inferenceInterface} from './core.mjs';
import {AgarthaRenderer} from './render.mjs';
import {AgarthaCanvasFallback} from './fallback.mjs';
const SAVE_KEY='dnc:agartha-selma:local-v1';
const $=id=>document.getElementById(id);
const memoryLog=$('memoryLog'),decisionLog=$('decisionLog'),status=$('status'),telemetry=$('telemetry');
const stored=()=>{try{return localStorage.getItem(SAVE_KEY)}catch{return null}};
let state=createDreamachine({seed:4301,playerName:'Crow Explorer',saved:stored()}),renderer=null,stopped=false,last=performance.now(),persistence=0,uiClock=0,pitch=.04,liquid=.78;
const held=new Set();let pointer=null;
$('badge').textContent='Selma° Local Director · game state only';
$('provider').textContent=inferenceInterface().provider;
try {renderer=new AgarthaRenderer($('view'));$('renderMode').textContent='GPU liquid-fractal shader';}catch(e){renderer=new AgarthaCanvasFallback($('view'));$('renderMode').textContent='Canvas2D fallback';status.textContent=`WebGL2 unavailable, using a CPU-rendered visual fallback. ${e.message}`;}
const bindHold=()=>document.querySelectorAll('[data-hold]').forEach(button=>{
 const key=button.dataset.hold;button.addEventListener('pointerdown',e=>{e.preventDefault();held.add(key);button.setPointerCapture(e.pointerId);});
 for(const ev of ['pointerup','pointercancel','lostpointercapture'])button.addEventListener(ev,()=>held.delete(key));});
bindHold();
window.addEventListener('keydown',e=>{const k=e.key.toLowerCase();if(['w','a','s','d','arrowup','arrowdown','arrowleft','arrowright','shift'].includes(k)){e.preventDefault();held.add(k);}if(e.repeat)return;
 if(k==='f')action('interact');if(k==='r')action('survey');if(k==='l')action('dream');});
window.addEventListener('keyup',e=>held.delete(e.key.toLowerCase()));
window.addEventListener('blur',()=>held.clear());
$('view').addEventListener('pointerdown',e=>{pointer={x:e.clientX,y:e.clientY,id:e.pointerId};$('view').setPointerCapture(e.pointerId);});
$('view').addEventListener('pointermove',e=>{if(!pointer||pointer.id!==e.pointerId)return;
 const dx=e.clientX-pointer.x,dy=e.clientY-pointer.y;state.player.yaw+=dx*.006;pitch=Math.min(.20,Math.max(-.58,pitch-dy*.002));pointer.x=e.clientX;pointer.y=e.clientY;});
for(const ev of ['pointercancel','pointerup','lostpointercapture'])$('view').addEventListener(ev,()=>pointer=null);
$('survey').addEventListener('click',()=>action('survey'));$('dream').addEventListener('click',()=>action('dream'));$('interact').addEventListener('click',()=>action('interact'));
$('motion').addEventListener('change',()=>{setLiveMessage($('motion').checked?'Reduced motion on. Fractal animation frozen.':'Full motion restored.');});
$('liquid').addEventListener('input',e=>{liquid=+e.target.value;$('liquidValue').textContent=`${Math.round(liquid*100)}%`;});
$('clear').addEventListener('click',()=>{if(!confirm('Clear Selma game memory? Loot and location are retained.'))return;state=resetMemory(state);persist();refresh();});
$('restart').addEventListener('click',()=>{if(!confirm('Restart this local expedition, including loot and memory?'))return;state=createDreamachine({seed:4301});persist();refresh();});
$('export').addEventListener('click',()=>{const blob=new Blob([exportDreamachine(state)],{type:'application/json'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download='DNC_Selma_Dreamachine_Local_Save.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),2000);});
$('memoryToggle').addEventListener('click',()=>{const p=$('memoryPanel');p.hidden=!p.hidden;$('memoryToggle').setAttribute('aria-expanded',String(!p.hidden));});
function setLiveMessage(message){status.textContent=message;}
function persist(){try{localStorage.setItem(SAVE_KEY,exportDreamachine(state));}catch{setLiveMessage('Local save unavailable: browser storage blocked or full. Export JSON manually.');}}
function action(k){state=actDreamachine(state,k);setLiveMessage(state.latest);persist();refresh();}
function refresh(){const ob=observe(state),p=state.player,dm=state.dm;
 $('region').textContent=ob.location.replaceAll('-',' ');$('height').textContent=`${ob.elevation.toFixed(1)} m`;
 $('temperature').textContent=`${ob.temperature.toFixed(1)} °C`;
 $('coordinates').textContent=`X ${p.x.toFixed(1)} / Z ${p.z.toFixed(1)}`;
 $('loot').textContent=`${p.loot}`;
 $('dreamAmount').textContent=`${Math.round(dm.dream*100)}%`;
 $('directive').textContent=dm.directive;
 $('reason').textContent=`Reason: ${dm.reason} · ${dm.intent}`;
 $('memoryCount').textContent=String(state.memory.length);
 $('timeline').textContent=`${state.tick.toFixed(1)} s`;
 const trace=dm.trace.length?dm.trace:inferDirector(state).slice(0,4);
 decisionLog.replaceChildren();for(const t of trace){const row=document.createElement('div');row.className='trace';row.textContent=`${t.id} · ${t.score.toFixed(1)} · ${t.reason}`;decisionLog.append(row);}
 memoryLog.replaceChildren();for(const item of state.memory.slice(-12).reverse()){const row=document.createElement('div');row.className='memo';row.textContent=`${item.tick.toFixed(1)}s · ${item.type} · ${item.narration||item.action}`;memoryLog.append(row);}
 telemetry.textContent=`Virtual telemetry • ${ob.climate} · slope ${ob.terrainGrade} · ${state.visited.length} regions visited · ${ob.gravityMode} · zero external sensors`;
}
refresh();setLiveMessage(state.latest);
function loop(now){if(stopped)return;const dt=Math.min(.05,(now-last)/1000);last=now;
 const h=x=>held.has(x),forward=+(h('w')||h('arrowup'))-+(h('s')||h('arrowdown'));
 const strafe=+(h('d'))-+(h('a')),turn=+(h('arrowright'))-+(h('arrowleft'));
 state=stepDreamachine(state,{dt,forward,strafe,turn,run:h('shift')});
 try{renderer?.render(state,{liquid,reducedMotion:$('motion').checked,pitch});}catch(e){if(renderer){renderer.dispose();renderer=new AgarthaCanvasFallback($('view'));$('renderMode').textContent='Canvas2D fallback';setLiveMessage(`GPU render failed; recovered using CPU view. ${e.message}`);}}
 uiClock+=dt;persistence+=dt;
 if(uiClock>.22){refresh();uiClock=0;}
 if(persistence>3){persist();persistence=0;}
 requestAnimationFrame(loop);
}
requestAnimationFrame(loop);
window.addEventListener('pagehide',()=>{stopped=true;persist();renderer?.dispose();});