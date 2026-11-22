/** Dungeons & Crows: Selma Dreamachine Core. Pure deterministic GAME-STATE inference.
 * No model calls, physical-world sensors, player surveillance, or server authority.
 */
export const SCHEMA = 'dnc-selma-dreamachine/1';
export const MEMORY_LIMIT = 96;
export const WORLD_LIMITS = Object.freeze({ minX:-65,maxX:65,minZ:-245,maxZ:12 });
export const LOCATIONS = Object.freeze([
 {id:'frost-fjord',title:'The Frostfjord',start: -45,climate:'glacial',tag:'water'},
 {id:'sunward-spire',title:'The Sunward Spire',start:-113,climate:'arid',tag:'crystal'},
 {id:'great-fracture',title:'The Great Fracture',start:-157,climate:'frozen',tag:'rift'},
 {id:'second-crown',title:'The Second Crown',start:-246,climate:'warm',tag:'highland'}
]);
const cap=(n,a,b)=>Math.min(b,Math.max(a,Number.isFinite(n)?n:a));
const hx=(s)=>{let h=2166136261;for(const c of String(s)){h^=c.charCodeAt(0);h=Math.imul(h,16777619);}return h>>>0;};
export function regionAt(z){return LOCATIONS.find(r=>z>=r.start)?.id||'second-crown';}
export function terrainHeight(x,z){
 const d=-cap(z,-1000,1000),xx=cap(x,-500,500);
 const gaussian=(c,w,a)=>a*Math.exp(-(((d-c)/w)**2));
 const valley= 2.0 + gaussian(80,30,58) + gaussian(187,29,53) - gaussian(133,13,18);
 const sideRidge=13*Math.exp(-(((Math.abs(xx)-30)/15)**2));
 const surface=1.1*Math.sin(xx*.16+d*.075)*Math.sin(d*.06)+.6*Math.sin(xx*.46+d*.03);
 return Math.max(-8,valley+sideRidge+surface);
}
export function slopeAt(x,z){const e=.45;return {x:(terrainHeight(x+e,z)-terrainHeight(x-e,z))/(2*e),z:(terrainHeight(x,z+e)-terrainHeight(x,z-e))/(2*e)};}
export function localClimate(x,z){const height=terrainHeight(x,z),region=regionAt(z);const irradiance=cap((height+8)/74,0,1);
 const base=region==='frost-fjord'?-12:region==='great-fracture'?-24:region==='sunward-spire'?26:16;
 const temperature=base+ (irradiance-.35)*33;
 return {region,height,temperature:+temperature.toFixed(1),irradiance:+irradiance.toFixed(2),frozen:temperature<=0};
}
export function createDreamachine({seed=431,playerName='Explorer',saved}={}){
 if(saved){try{return restoreDreamachine(saved)}catch{ /* invalid or old save deliberately not trusted */ }}
 const x=0,z=4;return {schema:SCHEMA,seed:hx(seed),tick:0,player:{name:String(playerName).slice(0,38),x,z,y:terrainHeight(x,z),yaw:0,health:100,stamina:100,loot:0},
 dm:{mode:'local-deterministic',lastEventTick:-999,reason:'Initial expedition',intent:'observe',dream:0,stability:1,preset:'tidal-lattice',directive:'The fjord remembers its last thaw.',trace:[]},
 memory:[],visited:['frost-fjord'],stats:{surveys:0,treasures:0,anomalies:0,travels:0},flags:{treasureClaimed:false},latest:'Fjord expedition ready. All surroundings are simulated game data.'};
}
function appendMemory(s,event){const id=`${s.tick.toFixed(2)}-${s.memory.length?hx(s.memory[s.memory.length-1].id):0}`;
 s.memory=[...s.memory,{id,tick:+s.tick.toFixed(2),region:regionAt(s.player.z),...event}].slice(-MEMORY_LIMIT);}
function roll(seed,step,tag){return hx(`${seed}|${Math.floor(step)}|${tag}`)/4294967295;}
export function observe(s){const p=s.player,c=localClimate(p.x,p.z),slope=slopeAt(p.x,p.z);
 return {location:c.region,climate:c.temperature>23?'sunlit-arid':c.frozen?'icy':'temperate',temperature:c.temperature,elevation:+c.height.toFixed(1),sunVisible:c.height>28,terrainGrade:+Math.hypot(slope.x,slope.z).toFixed(2),nearTreasure:!s.flags.treasureClaimed&&Math.hypot(p.x-6,p.z+187)<11,nearFjord:p.z>-36,nearFracture:p.z< -112&&p.z>-158,gravityMode:'9.81-m/s²-local-test-assumption',playerHealth:p.health,stamina:p.stamina,memoryCount:s.memory.length,visited:s.visited.length};
}
export function inferDirector(s,ob=observe(s)){
 const count=tag=>s.memory.filter(e=>e.type===tag).length;
 const proposals=[
  {id:'guard',label:'Preserve motion clarity',score:1,reason:'Always keep a readable path and playable collision',kind:'safety',preset:'tidal-lattice'},
  {id:'ice',label:'The glacier opens a listening chamber',score:ob.climate==='icy'?4.0:0,reason:'Ice temperature and route exposure',kind:'environment',preset:'crystal-drift'},
  {id:'solar',label:'A silent solar plume lights the spire',score:ob.sunVisible?4.5:0,reason:'Sunward elevation and light exposure',kind:'environment',preset:'solar-veil'},
  {id:'rift',label:'The fracture remembers a forgotten bridge',score:ob.nearFracture?5:0,reason:'Fracture proximity',kind:'discovery',preset:'mirror-tide'},
  {id:'cache',label:'A mineral seam conceals a crow cache',score:ob.nearTreasure?6:0,reason:'World-known loot position',kind:'discovery',preset:'crystal-drift'},
  {id:'echo',label:'A route echo traces the last few footsteps',score:1+Math.min(2,count('travel')*.12),reason:'Travel memory, bounded replay',kind:'memory',preset:'mirror-tide'}
 ];
 return proposals.map(x=>({...x,score:+x.score.toFixed(2)})).sort((a,b)=>b.score-a.score||a.id.localeCompare(b.id));
}
function directorEvent(s,reason){const ob=observe(s),rank=inferDirector(s,ob), top=rank[0];
 s.dm={...s.dm,lastEventTick:s.tick,reason,intent:top.kind, directive:top.label,preset:top.preset,
 dream:cap(top.id==='guard'?.12: top.id==='cache'?.72:top.id==='rift'?.66:top.id==='solar'?.50:.44,0,.78),
 stability:cap(.95-(top.id==='rift'?.18:.07),.7,1),trace:rank.slice(0,4).map(({id,score,reason})=>({id,score,reason}))};
 appendMemory(s,{type:'dm',action:top.id,narration:top.label,why:reason});
 s.latest=`Selma°: ${top.label}. (${reason})`;
 return top;
}
export function stepDreamachine(previous,{dt=1/60,forward=0,strafe=0,turn=0,run=false}={}){
 const s=structuredClone(previous),t=cap(dt,0,.1); s.tick=+(s.tick+t).toFixed(3);
 const p=s.player; p.yaw+=cap(turn,-2,2)*t;const a=cap(forward,-1,1),b=cap(strafe,-1,1);const norm=Math.max(1,Math.hypot(a,b));const speed=(run?19:12.5)*(cap(1/(1+Math.abs(slopeAt(p.x,p.z).z)*.18),.6,1));
 p.x=cap(p.x+((Math.cos(p.yaw)*b+Math.sin(p.yaw)*a)/norm)*speed*t,WORLD_LIMITS.minX,WORLD_LIMITS.maxX);
 p.z=cap(p.z+((Math.sin(p.yaw)*b-Math.cos(p.yaw)*a)/norm)*speed*t,WORLD_LIMITS.minZ,WORLD_LIMITS.maxZ);
 p.y=terrainHeight(p.x,p.z);p.stamina=cap(p.stamina+(run? -6:9)*t,0,100);
 const r=regionAt(p.z);if(!s.visited.includes(r)){s.visited=[...s.visited,r];appendMemory(s,{type:'discovery',action:'entered-region',narration:r});directorEvent(s,'new-location');}
 if((a||b)&& Math.floor(s.tick/6)!==Math.floor((s.tick-t)/6)) {s.stats.travels++;appendMemory(s,{type:'travel',action:'route-progress',narration:`Reached ${r}; elevation ${p.y.toFixed(1)} m`});}
 s.dm.dream=Math.max(0,s.dm.dream-t*.035);
 if(s.tick-s.dm.lastEventTick>19 && s.tick>8){if(roll(s.seed,s.tick,'world-event')>.38)directorEvent(s,'world-state-and-player-history');else s.dm.lastEventTick=s.tick;}
 return s;
}
export function actDreamachine(previous,action){const s=structuredClone(previous);const ob=observe(s);
 switch(action){
 case 'survey':s.stats.surveys++;appendMemory(s,{type:'survey',action:'read-world',narration:`${ob.location}: ${ob.temperature}°C, grade ${ob.terrainGrade}`});directorEvent(s,'player-survey');break;
 case 'dream':s.stats.anomalies++;appendMemory(s,{type:'anomaly',action:'voluntary-dream',narration:'Player requested a reversible visual phase'});directorEvent(s,'player-triggered-dream');s.dm.dream=cap(s.dm.dream+.26,0,.78);break;
 case 'interact':if(ob.nearTreasure){s.flags.treasureClaimed=true;s.player.loot++;s.stats.treasures++;appendMemory(s,{type:'loot',action:'claimed-crow-cache',narration:'Mineral Crow Cache claimed'});directorEvent(s,'treasure-discovered');s.latest='You recovered the Mineral Crow Cache. Saved into session state.';}else{appendMemory(s,{type:'inspect',action:'look-around',narration:'No collectible within reach'});directorEvent(s,'exploration-inspection');}break;
 default:throw new Error('Unsupported player action');
 }return s;
}
export function resetMemory(previous){const s=structuredClone(previous);s.memory=[];s.dm.trace=[];s.dm.reason='Memory cleared by player';s.latest='Selma local memory was cleared. World and loot remain unchanged.';return s;}
export function exportDreamachine(s){return JSON.stringify(s,null,2);}
export function restoreDreamachine(input){const s=typeof input==='string'?JSON.parse(input):structuredClone(input);
 if(s?.schema!==SCHEMA||!s.player||!Array.isArray(s.memory)||s.memory.length>MEMORY_LIMIT||!Array.isArray(s.visited)||s.visited.length>32||!s.dm||!s.stats)throw new Error('Invalid Dreamachine save schema');
 for(const n of [s.player.x,s.player.z,s.player.y,s.player.yaw,s.tick,s.player.loot])if(!Number.isFinite(n))throw new Error('Invalid numeric state');
 if(s.player.x<WORLD_LIMITS.minX||s.player.x>WORLD_LIMITS.maxX||s.player.z<WORLD_LIMITS.minZ||s.player.z>WORLD_LIMITS.maxZ)throw new Error('Out of world bounds');
 s.player.name=String(s.player.name).slice(0,38);s.player.y=terrainHeight(s.player.x,s.player.z);
 // Serialized output is read only as data; never used as code or instructions.
 return s;
}
export function inferenceInterface(){return {version:'selma-inference-contract/1',provider:'deterministic-local',inputs:['game-position','player-actions','virtual-climate','recent-game-events'],outputs:['ranked-directives','bounded-visual-preset','auditable-trace'],externalSensors:false,networkCalls:false,autonomousActions:false};}