"""Build a local comparison gallery from the actual gameplay-camera capture files."""
from pathlib import Path

folder = Path(__file__).resolve().parents[1] / "Build/VP1BC/Captures"
views = [("01-Kitchen-Grill-Prep", "Cocina: Grill y Prep"), ("02-Kitchen-PASS", "Cocina: PASS"),
         ("03-Customers-Counter", "Clientes y mostrador"), ("04-ColdStorage", "Fridge / Freezer"),
         ("05-Power-OFF", "Power OFF"), ("06-Grill-Close", "Plancha de cerca"),
         ("07-Raw-Cooked-Burnt-Patty", "Raw / Cooked / Burnt: unidades originales"),
         ("08-Procurement", "Procurement: anexo conservado"), ("09-Food-First-Pass", "Bun, Patty, Cheese y Plate"),
         ("10-M17-Dynamic-Dirt", "Suciedad dinámica M17 al 100 %")]
html = ['<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width">',
        '<title>VP1B/C · Capturas de gameplay</title><style>body{margin:32px;background:#151817;color:#e4e4dd;font:16px system-ui;line-height:1.5}main{max-width:1500px;margin:auto}h1{font-size:26px}h2{font-size:20px;margin-top:42px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img{width:100%;display:block}figcaption{color:#aeb9b0}a{color:#c5d2b3}@media(max-width:800px){.pair{grid-template-columns:1fr}}</style><main>',
        '<h1>VP1B/C · Restaurant art pass</h1><p>PlayerCamera real durante Play · 1600 × 900 · mismo FOV y poses entre cada par. Compras y cocción reales; drivers y posiciones pausados temporalmente para capturar. Sin guardar escenas.</p>',
        '<p>La referencia conserva VP1A; Art incluye su calibración de exposición. Las imágenes no contienen el HUD OnGUI. Aceptación artística y prueba de controles humanas pendientes.</p>']
for stem, title in views:
    html.append(f'<h2>{title}</h2><div class="pair">')
    for suffix, label in [("VP1A-Before", "Antes: VP1A"), ("Art", "VP1B/C")]:
        image = folder / f"{stem}-{suffix}.png"
        if image.exists():
            html.append(f'<figure><a href="{image.name}"><img loading="lazy" src="{image.name}" alt="{title}: {label}"></a><figcaption>{label}</figcaption></figure>')
    html.append('</div>')
html.append('</main></html>')
(folder / 'index.html').write_text('\n'.join(html), encoding='utf-8')
print(folder / 'index.html')
