import {terrainHeight,slopeAt,localClimate} from './core.mjs';

const SKY_VS=`#version 300 es
precision highp float; out vec2 vUv;
void main(){vec2 p=vec2((gl_VertexID==1||gl_VertexID==2)?3.:-1.,gl_VertexID>=2?3.:-1.);vUv=p*.5+.5;gl_Position=vec4(p,0.,1.);}`;
const SKY_FS=`#version 300 es
precision highp float; in vec2 vUv;out vec4 fragColor;
uniform float uTime,uYaw,uPitch,uDream,uSeed;
uniform vec2 uWorld;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);return mix(mix(hash(i),hash(i+vec2(1.,0.)),f.x),mix(hash(i+vec2(0.,1.)),hash(i+vec2(1.,1.)),f.x),f.y);}
void main(){vec2 uv=vUv;
 vec3 deep=vec3(.033,.095,.18),light=vec3(.23,.35,.44);
 vec3 sky=mix(deep,light,smoothstep(.12,.95,uv.y));
 float q=abs(uv.x-.5)*2.;
 float coast=.96-.30*pow(q,1.5)+.026*sin(uv.x*31.+uWorld.x*.001);
 float coastline=smoothstep(coast-.015,coast+.024,uv.y);
 vec3 distant=vec3(.22,.32,.29)+vec3(.075,.11,.13)*noise(uv*vec2(75.,42.)+uWorld*.002);
 sky=mix(sky,distant,coastline*.88);
 float sea=smoothstep(coast-.08,coast-.05,uv.y)*(1.-smoothstep(coast-.02,coast+.01,uv.y));
 sky=mix(sky,vec3(.18,.48,.53)+vec3(.02,.08,.09)*sin(uv.y*240.+uv.x*12.),sea*.72);
 float sunX=.5+.16*sin(uYaw),sunY=.64-.08*sin(uPitch);
 float d=length((uv-vec2(sunX,sunY))*vec2(1.75,1.));
 float flare=noise((uv-vec2(sunX,sunY))*60.+vec2(uTime*.07,uSeed));
 float ring=exp(-pow((d-.060)/.022,2.));
 sky+=vec3(.75,.42,.18)*ring*.45;
 sky+=vec3(.45,.28,.12)*exp(-d*17.)*.60;
 float disc=1.-smoothstep(.046,.050,d);
 sky=mix(sky,vec3(1.,.88,.53)+vec3(.06,.04,-.10)*(flare-.5),disc);
 // Slowly rotating sun with a partial line-of-sight shadow: illustrative, not a validated eclipse model.
 float occluder=1.-smoothstep(.017,.021,length((uv-vec2(sunX+.011+.009*sin(uWorld.x*.003),sunY+.007))*vec2(1.75,1.)));
 sky=mix(sky,vec3(.075,.092,.10),occluder*.91);
 float dreamHaze=uDream*.10*noise(uv*vec2(9.,12.)+uTime*.05);
 fragColor=vec4(clamp(sky+vec3(dreamHaze,dreamHaze*.8,dreamHaze*1.3),0.,1.),1.);
}`;
const GEO_VS=`#version 300 es
precision highp float;layout(location=0)in vec3 aPosition;layout(location=1)in vec3 aColor;layout(location=2)in vec3 aNormal;
uniform mat4 uVP;out vec3 vColor;out vec3 vNormal;out float vFog;out vec3 vWorld;
void main(){gl_Position=uVP*vec4(aPosition,1.);vColor=aColor;vNormal=aNormal;vWorld=aPosition;vFog=clamp(-aPosition.z/290.,0.,1.);}`;
const GEO_FS=`#version 300 es
precision highp float;in vec3 vColor,vNormal,vWorld;in float vFog;uniform float uTime,uDream;out vec4 fragColor;
void main(){vec3 N=normalize(vNormal),L=normalize(vec3(.22,.84,.4));float shade=.34+.66*max(0.,dot(N,L));
 float sparkle=pow(max(0.,sin(vWorld.x*.58+vWorld.z*.68+uTime*.25)),18.);
 vec3 color=vColor*shade+sparkle*.027;
 color=mix(color,vec3(.17,.28,.34),vFog*.24);
 fragColor=vec4(color,1.);
}`;
const LIQUID_FS=`#version 300 es
precision highp float;in vec2 vUv;out vec4 fragColor;
uniform sampler2D uScene;uniform vec2 uResolution;
uniform float uTime,uDream,uLiquid,uMotion,uStress,uSeed,uClarity,uMemory;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);return mix(mix(hash(i),hash(i+vec2(1.,0.)),f.x),mix(hash(i+vec2(0.,1.)),hash(i+vec2(1.,1.)),f.x),f.y);}
float fbm(vec2 p){float a=.5,s=0.;mat2 rot=mat2(.8,-.6,.6,.8);for(int i=0;i<5;i++){s+=a*noise(p);p=rot*p*2.03+11.;a*=.5;}return s;}
void main(){vec2 uv=vUv,aspect=vec2(uResolution.x/max(1.,uResolution.y),1.);
 vec2 p=(uv-.5)*aspect;float t=uTime*(.10+.33*uMotion);
 float n1=fbm(p*4.4+vec2(t,uSeed*11.));
 float n2=fbm(p*7.2+vec2(-t*.72,t*.35)+n1*.8);
 vec2 flow=vec2(n1-.5,n2-.5);
 float safeArea=1.-smoothstep(uClarity,uClarity+.30,length(p));
 float intensity=clamp(uDream,0.,.78)*clamp(uLiquid,0.,1.)*(1.-safeArea*.88);
 vec2 displacement=flow*intensity*.078;
 vec2 suv=clamp(uv+displacement,vec2(.002),vec2(.998));
 vec3 color=texture(uScene,suv).rgb;
 float dreamLattice=sin((length(p*1.9)+n2*.14)*49.+n1*7.-t*2.);
 float mask=smoothstep(.72,1.,dreamLattice)*intensity;
 vec3 tint=mix(vec3(.08,.42,.46),vec3(.72,.38,.18),clamp(uStress,0.,1.));
 color+=tint*mask*.13;
 // Keep navigable center clear. Motion does not alter game geometry, collision, controls or loot.
 color=mix(color,texture(uScene,uv).rgb,safeArea*.90);
 float baseLum=dot(texture(uScene,uv).rgb,vec3(.2126,.7152,.0722));
 float outLum=max(.03,dot(color,vec3(.2126,.7152,.0722)));
 color*=mix(1.,clamp(baseLum/outLum,.77,1.24),.33*intensity);
 fragColor=vec4(clamp(color,0.,1.),1.);
}`;
function shader(gl,type,source){const s=gl.createShader(type);gl.shaderSource(s,source);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS)){const err=gl.getShaderInfoLog(s);gl.deleteShader(s);throw Error(err);}return s;}
function program(gl,vs,fs){const p=gl.createProgram(),v=shader(gl,gl.VERTEX_SHADER,vs),f=shader(gl,gl.FRAGMENT_SHADER,fs);gl.attachShader(p,v);gl.attachShader(p,f);gl.linkProgram(p);gl.deleteShader(v);gl.deleteShader(f);if(!gl.getProgramParameter(p,gl.LINK_STATUS)){const err=gl.getProgramInfoLog(p);gl.deleteProgram(p);throw Error(err);}return p;}
function m4mul(a,b){const o=new Float32Array(16);for(let c=0;c<4;c++)for(let r=0;r<4;r++)o[c*4+r]=a[r]*b[c*4]+a[4+r]*b[c*4+1]+a[8+r]*b[c*4+2]+a[12+r]*b[c*4+3];return o;}
function perspective(aspect){const f=1/Math.tan(Math.PI/5.5),near=.15,far=700,k=1/(near-far);return new Float32Array([f/aspect,0,0,0,0,f,0,0,0,0,(far+near)*k,-1,0,0,2*far*near*k,0]);}
function view(x,y,z,yaw,pitch){const cy=Math.cos(yaw),sy=Math.sin(yaw),cp=Math.cos(pitch),sp=Math.sin(pitch);
 const forward=[sy*cp,sp,-cy*cp],right=[cy,0,sy],up=[-sy*sp,cp,cy*sp];
 return new Float32Array([right[0],up[0],-forward[0],0,right[1],up[1],-forward[1],0,right[2],up[2],-forward[2],0,-(right[0]*x+right[1]*y+right[2]*z),-(up[0]*x+up[1]*y+up[2]*z),forward[0]*x+forward[1]*y+forward[2]*z,1]);}
function clamp(v,min,max){return Math.min(max,Math.max(min,v));}
function mesh(){const a=[];const add=(x,y,z,color,n)=>a.push(x,y,z,...color,...n);
 const terrainVertex=(x,z)=>{const y=terrainHeight(x,z),sl=slopeAt(x,z),n=[-sl.x,1,-sl.z],nn=Math.hypot(...n);const h=clamp((y+4)/71,0,1),frozen=z> -47||(z< -118&&z> -164);
 const c=frozen?[.22+h*.19,.38+h*.18,.42+h*.14]:h>.54?[.61+h*.18,.44+h*.13,.29+h*.06]:[.24+h*.19,.38+h*.12,.27+h*.11];return{x,y,z,color:c,normal:n.map(q=>q/nn)};};
 const push=(v)=>add(v.x,v.y,v.z,v.color,v.normal);
 for(let x=-65;x<65;x+=3.7)for(let z=-245;z<15;z+=3.7){const v=[terrainVertex(x,z),terrainVertex(x+3.7,z),terrainVertex(x+3.7,z+3.7),terrainVertex(x,z+3.7)];for(const i of [0,1,2,0,2,3])push(v[i]);}
 const crystals=[[-34,-42,21],[-28,-89,32],[34,-72,28],[-40,-176,19],[36,-218,28],[20,-120,14],[-35,-137,17],[40,-120,15]];
 for(let i=0;i<crystals.length;i++){const [cx,cz,h]=crystals[i],base=terrainHeight(cx,cz),r=4.5+i%3;const pts=[[cx-r,base,cz-r],[cx+r,base,cz-r],[cx+r,base,cz+r],[cx-r,base,cz+r]],tip=[cx,base+h,cz];for(let j=0;j<4;j++){const c=[.32+i%3*.06,.68-i%3*.04,.66+i%2*.11],n=[Math.sin(j*1.57),.46,Math.cos(j*1.57)];add(...pts[j],c,n);add(...pts[(j+1)%4],c,n);add(...tip,c,n);}}
 // Faceted water surface at fjord inlet. Real flows are not claimed in this geometry graybox.
 const wc=[.17,.48,.55],wn=[0,1,0],y=2.45;for(const z of [-8,-4,0,4,8,12])for(let x=-16;x<16;x+=4){for(const p of [[x,y,z],[x+4,y,z],[x+4,y,z+4],[x,y,z],[x+4,y,z+4],[x,y,z+4]])add(...p,wc,wn);}
 return new Float32Array(a);
}
export class AgarthaRenderer {
 constructor(canvas){this.canvas=canvas;const gl=canvas.getContext('webgl2',{antialias:true,alpha:false});if(!gl)throw Error('WebGL2 unavailable');this.gl=gl;
 this.sky=program(gl,SKY_VS,SKY_FS);this.geo=program(gl,GEO_VS,GEO_FS);this.post=program(gl,SKY_VS,LIQUID_FS);
 this.terrain=gl.createBuffer();this.vertices=mesh();gl.bindBuffer(gl.ARRAY_BUFFER,this.terrain);gl.bufferData(gl.ARRAY_BUFFER,this.vertices,gl.STATIC_DRAW);
 this.vao=gl.createVertexArray();gl.bindVertexArray(this.vao);gl.bindBuffer(gl.ARRAY_BUFFER,this.terrain);
 for(let i=0;i<3;i++){gl.enableVertexAttribArray(i);gl.vertexAttribPointer(i,3,gl.FLOAT,false,36,i*12);}gl.bindVertexArray(null);
 this.skyVao=gl.createVertexArray();this.texture=gl.createTexture();this.fbo=gl.createFramebuffer();this.depth=gl.createRenderbuffer();this.width=0;this.height=0;
 }
 resize(){const gl=this.gl,dpr=Math.min(1.5,devicePixelRatio||1),w=Math.max(4,Math.round(this.canvas.clientWidth*dpr)),h=Math.max(4,Math.round(this.canvas.clientHeight*dpr));if(w===this.width&&h===this.height)return;
 this.canvas.width=w;this.canvas.height=h;this.width=w;this.height=h;
 gl.bindTexture(gl.TEXTURE_2D,this.texture);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA8,w,h,0,gl.RGBA,gl.UNSIGNED_BYTE,null);
 gl.bindRenderbuffer(gl.RENDERBUFFER,this.depth);gl.renderbufferStorage(gl.RENDERBUFFER,gl.DEPTH_COMPONENT16,w,h);
 gl.bindFramebuffer(gl.FRAMEBUFFER,this.fbo);gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.COLOR_ATTACHMENT0,gl.TEXTURE_2D,this.texture,0);gl.framebufferRenderbuffer(gl.FRAMEBUFFER,gl.DEPTH_ATTACHMENT,gl.RENDERBUFFER,this.depth);
 if(gl.checkFramebufferStatus(gl.FRAMEBUFFER)!==gl.FRAMEBUFFER_COMPLETE)throw Error('Scene render target incomplete');gl.bindFramebuffer(gl.FRAMEBUFFER,null);
 }
 render(state,{liquid=.75,reducedMotion=false,pitch=.04}={}){
 this.resize();const gl=this.gl,p=state.player,dm=state.dm,t=state.tick;
 gl.bindFramebuffer(gl.FRAMEBUFFER,this.fbo);gl.viewport(0,0,this.width,this.height);gl.disable(gl.DEPTH_TEST);gl.clearColor(.03,.08,.14,1);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);
 const uf=(program,name,...v)=>{const loc=gl.getUniformLocation(program,name);if(loc===null)return;v.length===2?gl.uniform2f(loc,...v):gl.uniform1f(loc,v[0]);};
 gl.useProgram(this.sky);gl.bindVertexArray(this.skyVao);uf(this.sky,'uTime',t);uf(this.sky,'uYaw',p.yaw);uf(this.sky,'uPitch',pitch);uf(this.sky,'uDream',dm.dream);uf(this.sky,'uSeed',(state.seed%100)/100);uf(this.sky,'uWorld',p.x,p.z);gl.drawArrays(gl.TRIANGLES,0,3);
 gl.enable(gl.DEPTH_TEST);gl.useProgram(this.geo);gl.bindVertexArray(this.vao);
 const back=14,camX=p.x-Math.sin(p.yaw)*back,camY=p.y+4.4,camZ=p.z+Math.cos(p.yaw)*back;
 const vp=m4mul(perspective(this.width/this.height),view(camX,camY,camZ,p.yaw,pitch));gl.uniformMatrix4fv(gl.getUniformLocation(this.geo,'uVP'),false,vp);uf(this.geo,'uTime',t);uf(this.geo,'uDream',dm.dream);gl.drawArrays(gl.TRIANGLES,0,this.vertices.length/9);
 gl.bindFramebuffer(gl.FRAMEBUFFER,null);gl.viewport(0,0,this.width,this.height);gl.disable(gl.DEPTH_TEST);gl.useProgram(this.post);gl.bindVertexArray(this.skyVao);
 gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,this.texture);gl.uniform1i(gl.getUniformLocation(this.post,'uScene'),0);
 uf(this.post,'uResolution',this.width,this.height);uf(this.post,'uTime',t);uf(this.post,'uDream',dm.dream);uf(this.post,'uLiquid',liquid);uf(this.post,'uMotion',reducedMotion?0:1);uf(this.post,'uStress',dm.intent==='discovery'?.9:.2);uf(this.post,'uSeed',state.seed%1000/1000);uf(this.post,'uClarity',.24);uf(this.post,'uMemory',Math.min(1,state.memory.length/96));gl.drawArrays(gl.TRIANGLES,0,3);gl.bindVertexArray(null);
 }
 dispose(){const gl=this.gl;for(const p of [this.sky,this.geo,this.post])gl.deleteProgram(p);gl.deleteBuffer(this.terrain);gl.deleteVertexArray(this.vao);gl.deleteVertexArray(this.skyVao);gl.deleteTexture(this.texture);gl.deleteRenderbuffer(this.depth);gl.deleteFramebuffer(this.fbo);}
}