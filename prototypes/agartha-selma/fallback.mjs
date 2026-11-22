import {terrainHeight,localClimate} from './core.mjs';
/** CPU Canvas2D fallback for restricted devices. No shader or 3D-GPU claims. */
export class AgarthaCanvasFallback {
 constructor(canvas){this.canvas=canvas;this.ctx=canvas.getContext('2d');if(!this.ctx)throw Error('Canvas2D unavailable');this.mobile=()=>canvas.clientWidth<620;}
 render(state,{liquid=.75,reducedMotion=false,pitch=.04}={}){
  const c=this.canvas,ctx=this.ctx,dpr=Math.min(1.4,devicePixelRatio||1),w=Math.max(320,Math.round(c.clientWidth*dpr)),h=Math.max(230,Math.round(c.clientHeight*dpr));
  if(c.width!==w||c.height!==h){c.width=w;c.height=h;}const p=state.player,dm=state.dm;
  const cy=Math.cos(p.yaw),sy=Math.sin(p.yaw),cp=Math.cos(pitch),sp=Math.sin(pitch),back=16;
  const cam=[p.x-sy*back,p.y+4.4,p.z+cy*back],focal=h*1.10;
  function proj(x,y,z){const dx=x-cam[0],dy=y-cam[1],dz=z-cam[2];
   const r=cy*dx+sy*dz,u=-sy*sp*dx+cp*dy+cy*sp*dz,d=sy*cp*dx+sp*dy-cy*cp*dz;
   if(d<.7)return null;return [w/2+focal*r/d,h/2-focal*u/d,d];}
  let sky=ctx.createLinearGradient(0,0,0,h);sky.addColorStop(0,'#071b2a');sky.addColorStop(.5,'#39717d');sky.addColorStop(1,'#d8b68e');ctx.fillStyle=sky;ctx.fillRect(0,0,w,h);
  // Artificial geode inner shell: rises toward the edges and becomes an upper ocean.
  ctx.fillStyle='#416f6f';ctx.beginPath();ctx.moveTo(0,0);ctx.lineTo(w,0);ctx.lineTo(w,h*.14);ctx.bezierCurveTo(w*.68,h*.02,w*.72,h*.10,w*.5,h*.105);ctx.bezierCurveTo(w*.28,h*.10,w*.29,h*.02,0,h*.14);ctx.fill();
  ctx.strokeStyle='#69b9b6';ctx.lineWidth=2;for(let i=0;i<5;i++){ctx.beginPath();ctx.moveTo(0,h*(.08+i*.018));ctx.quadraticCurveTo(w*.5,h*(.02+i*.012),w,h*(.08+i*.018));ctx.stroke();}
  const sx=w*(.50+.16*Math.sin(p.yaw));const sunY=h*(.30-.07*Math.sin(pitch));
  const halo=ctx.createRadialGradient(sx,sunY,0,sx,sunY,90*dpr);halo.addColorStop(0,'#fff9be');halo.addColorStop(.09,'#f8d288');halo.addColorStop(.16,'#da985a88');halo.addColorStop(1,'#aa713500');ctx.fillStyle=halo;ctx.fillRect(sx-90*dpr,sunY-90*dpr,180*dpr,180*dpr);
  ctx.beginPath();ctx.fillStyle='#f9dfa0';ctx.arc(sx,sunY,18*dpr,0,Math.PI*2);ctx.fill();ctx.fillStyle='#102e3c';ctx.beginPath();ctx.arc(sx+6*dpr,sunY+3*dpr,10*dpr,0,Math.PI*2);ctx.fill();
  const tris=[];
  const step=8;
  for(let x=-64;x<64;x+=step)for(let z=-245;z<14;z+=step){
   const a=proj(x,terrainHeight(x,z),z),b=proj(x+step,terrainHeight(x+step,z),z),cc=proj(x+step,terrainHeight(x+step,z+step),z+step),d=proj(x,terrainHeight(x,z+step),z+step);
   const climate=localClimate(x+step*.5,z+step*.5);const height=climate.height;
   const frozen=climate.region==='frost-fjord'||climate.region==='great-fracture';
   const green=frozen?`hsl(187 30% ${Math.min(62,35+height*.27)}%)`:height>24?`hsl(31 33% ${Math.min(66,25+height*.34)}%)`:`hsl(145 28% ${34+height*.2}%)`;
   for(const v of [[a,b,cc],[a,cc,d]])if(v.every(Boolean)){const av=v.reduce((s,t)=>s+t[2],0)/3;if(av<340)tris.push({v,depth:av,color:green});}
  }
  tris.sort((a,b)=>b.depth-a.depth);for(const t of tris){ctx.beginPath();t.v.forEach((v,i)=>i?ctx.lineTo(v[0],v[1]):ctx.moveTo(v[0],v[1]));ctx.closePath();ctx.fillStyle=t.color;ctx.fill();}
  // Crystals connect to physical terrain locations, not only overlay pixels.
  for(const [x,z,height] of [[-34,-42,21],[-28,-89,32],[34,-72,28],[-40,-176,19],[36,-218,28],[20,-120,14],[-35,-137,17],[40,-120,15]]){
   const foot=proj(x,terrainHeight(x,z),z),top=proj(x,terrainHeight(x,z)+height,z);if(!foot||!top)continue;
   const width=Math.min(20,focal*3.8/foot[2]);ctx.beginPath();ctx.moveTo(foot[0]-width,foot[1]);ctx.lineTo(top[0],top[1]);ctx.lineTo(foot[0]+width,foot[1]);ctx.closePath();ctx.fillStyle='#8dd2cc';ctx.fill();ctx.strokeStyle='#d1fff0';ctx.lineWidth=.7;ctx.stroke();}
  if(!state.flags.treasureClaimed){const v=proj(6,terrainHeight(6,-187)+1,-187);if(v){ctx.fillStyle='#ffeab0';ctx.beginPath();ctx.arc(v[0],v[1],Math.max(2,10*dpr/v[2]),0,2*Math.PI);ctx.fill();}}
  // Player stands in readable foreground as a human, not default crow-headed.
  const center=proj(p.x,p.y,p.z);if(center){const px=center[0],py=center[1],scale=Math.min(1.4,dpr);ctx.fillStyle='#0e1827';ctx.strokeStyle='#8aa9a8';ctx.lineWidth=1;
   ctx.beginPath();ctx.moveTo(px-8*scale,py);ctx.lineTo(px-7*scale,py-29*scale);ctx.lineTo(px,py-34*scale);ctx.lineTo(px+8*scale,py-26*scale);ctx.lineTo(px+11*scale,py);ctx.closePath();ctx.fill();ctx.stroke();
   ctx.fillStyle='#d6c7a8';ctx.beginPath();ctx.arc(px,py-37*scale,5*scale,0,Math.PI*2);ctx.fill();
   ctx.strokeStyle='#f2d29f';ctx.beginPath();ctx.moveTo(px+8*scale,py-26*scale);ctx.lineTo(px+18*scale,py-15*scale);ctx.stroke();}
  // Slow, bounded dream transition in the outer visual region (never moves collisions).
  if(dm.dream>.03){ctx.save();ctx.globalAlpha=dm.dream*liquid*.45;
   for(let i=0;i<9;i++){const y=h*(i+.5)/9;ctx.strokeStyle=i%2?'#b9a6ea':'#68e8e1';ctx.lineWidth=2;ctx.beginPath();for(let x=0;x<=w;x+=12){const amp=Math.max(0,Math.abs(x-w/2)/(w/2)-.22);const dy=Math.sin(x*.018+i+(!reducedMotion?state.tick*.31:0))*22*amp; x===0?ctx.moveTo(x,y+dy):ctx.lineTo(x,y+dy);}ctx.stroke();}ctx.restore();}
 }
 dispose(){}
}