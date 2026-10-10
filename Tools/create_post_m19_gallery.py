"""Build a local comparison gallery without altering captured pixels."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1] / "Build/PostM19"
names = [p.name.removesuffix("-ON.png") for p in sorted((root / "Before").glob("*-ON.png"))]
html = r"""<!doctype html><html lang="es"><meta charset="utf-8"><title>Post M19 - comparaciones</title>
<style>body{background:#171917;color:#eee;font:16px system-ui;margin:24px}header{position:sticky;top:0;background:#171917;padding:12px;z-index:2}button,select,input{font:inherit}article{margin:24px 0;padding:16px;border:1px solid #555}figure{margin:0;position:relative;aspect-ratio:16/9;background:#000}img{position:absolute;width:100%;height:100%;object-fit:contain}.before{clip-path:inset(0 50% 0 0)}h2{font-size:18px}p{max-width:900px}a{color:#b5d7b7}</style>
<header><h1>Estabilizacion posterior a M19</h1><label>Corriente <select id="power"><option>ON</option><option>OFF</option></select></label> <button id="left">Antes</button> <button id="both">Comparar</button> <button id="right">Despues</button></header>
<p>PlayerCamera real en Play, URP, 1600 x 900. Misma pose, FOV y luz por pareja. Izquierda: f6fb68c. Derecha: reparacion. Arrastrar el control de cada imagen. Abrir los PNG para inspeccion a resolucion completa. Las capturas conservan sus pixeles originales.</p><main></main>
<script>const names=DATA;
for(const name of names){let el=document.createElement('article');el.innerHTML=`<h2>${name}</h2><figure><img class="after" alt="Despues"><img class="before" alt="Antes"></figure><input type="range" min="0" max="100" value="50" aria-label="Comparacion ${name}"><p><a class="beforeLink" target="_blank">PNG antes</a> | <a class="afterLink" target="_blank">PNG despues</a></p>`;el.dataset.name=name;el.querySelector('input').oninput=e=>el.querySelector('.before').style.clipPath=`inset(0 ${100-e.target.value}% 0 0)`;document.querySelector('main').append(el)}
function update(){for(const el of document.querySelectorAll('article')){const n=el.dataset.name+'-'+document.querySelector('#power').value+'.png';el.querySelector('.before').src='Before/'+n;el.querySelector('.after').src='After/'+n;el.querySelector('.beforeLink').href='Before/'+n;el.querySelector('.afterLink').href='After/'+n}}
function slide(v){for(const el of document.querySelectorAll('article')){let r=el.querySelector('input');r.value=v;r.dispatchEvent(new Event('input'))}}
document.querySelector('#power').onchange=update;document.querySelector('#left').onclick=()=>slide(100);document.querySelector('#both').onclick=()=>slide(50);document.querySelector('#right').onclick=()=>slide(0);update();</script></html>"""
(root / "index.html").write_text(html.replace("DATA", json.dumps(names)), encoding="utf-8")
print(f"Gallery: {len(names)} pairs per power state, {root / 'index.html'}")
