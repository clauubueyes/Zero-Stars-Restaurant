"""Write a local before/after gallery from the real PlayerCamera captures."""
from pathlib import Path

folder = Path(__file__).resolve().parents[1] / 'Build/VisualPolish/Captures'
views = [('01-Hamburger', 'Hamburger'), ('02-Cheeseburger', 'Cheeseburger'),
         ('03-Grill-Clean', 'Grill Clean · 0 %'), ('04-Grill-Used', 'Grill Used · 18 %'),
         ('05-Grill-Dirty', 'Grill Dirty · 55 %'), ('06-Grill-Filthy', 'Grill Filthy · 95 %'),
         ('07-Prep-Dirty', 'Prep Dirty · 55 %'), ('08-Floor-Dirty', 'Floor Dirty · 55 %')]
html = ['<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width">',
        '<title>Visual polish · Hamburger y Dirt</title><style>body{margin:28px;background:#171917;color:#ece9df;font:16px system-ui;line-height:1.5}main{max-width:1500px;margin:auto}h1{font-size:27px}h2{font-size:20px;margin-top:40px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px}figure{margin:0}img{width:100%;display:block}figcaption{color:#b9c1b7}@media(max-width:800px){.pair{grid-template-columns:1fr}}</style><main>',
        '<h1>Polish visual · Hamburger y Dirt</h1><p>PlayerCamera durante Play · 1600 × 900 · ojo a 1,65 m · misma pose/FOV entre pares · misma iluminación VP1B/C. Sin HUD.</p>',
        '<p>Compras, cocción y confirmación reales. Staging temporal y drivers pausados para capturar; ningún guardado de escena. El antes restaura la representación de las mismas unidades/estados. La compactación se aplica al confirmar con F; la física conserva sus dimensiones.</p>']
for stem, title in views:
    html.append(f'<h2>{title}</h2><div class="pair">')
    for suffix, label in [('VP1BC-Before', 'Antes · VP1B/C'), ('Polish', 'Polish')]:
        name = f'{stem}-{suffix}.png'
        assert (folder/name).exists(), name
        html.append(f'<figure><a href="{name}"><img loading="lazy" src="{name}" alt="{title}: {label}"></a><figcaption>{label}</figcaption></figure>')
    html.append('</div>')
html.append('</main></html>')
(folder/'index.html').write_text('\n'.join(html), encoding='utf-8')
print(folder/'index.html')
