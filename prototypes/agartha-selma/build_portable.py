from pathlib import Path
import re
root=Path(__file__).parent
parts=[]
for name in ['core.mjs','render.mjs','fallback.mjs','main.mjs']:
 s=(root/name).read_text()
 s=re.sub(r'^import\s+.*?from\s+.*?;\s*$', '', s, flags=re.M)
 s=re.sub(r'\bexport\s+(?=(?:const|function|class|let|async))', '', s)
 parts.append('// SOURCE '+name+'\n'+s)
html=(root/'index.html').read_text().replace('<script type="module" src="./main.mjs"></script>','<script>\n'+ '\n\n'.join(parts)+'\n</script>')
(root/'DungeonsAndCrows_Agartha_SelmaDreamachine_Portable.html').write_text(html)
print('Created',root/'DungeonsAndCrows_Agartha_SelmaDreamachine_Portable.html', 'bytes=',len(html))