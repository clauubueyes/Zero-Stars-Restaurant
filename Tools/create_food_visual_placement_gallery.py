from pathlib import Path

folder = Path(__file__).resolve().parents[1]/'Build/FoodVisualPlacement/Captures'
names = sorted(p.name for p in (folder/'Before').glob('*.png'))
parts = ['<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Food visual placement</title>',
         '<style>body{background:#181a18;color:#eee;font:16px system-ui;max-width:1500px;margin:28px auto;padding:16px}img{width:100%}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px}figure{margin:0}h2{margin-top:36px}@media(max-width:800px){.pair{grid-template-columns:1fr}}</style>',
         '<h1>Food visual placement · b023fe8 / corrección</h1><p>PlayerCamera durante Play, misma iluminación, pose y FOV. Compras, Pickup, colocación asistida y simulación física reales; staging temporal sin guardar la escena.</p>']
for name in names:
    assert (folder/'After'/name).exists(), name
    parts.append('<h2>'+name.removesuffix('.png')+'</h2><div class="pair">')
    for phase, label in [('Before','Antes · b023fe8'),('After','Corrección')]:
        parts.append(f'<figure><a href="{phase}/{name}"><img src="{phase}/{name}" loading="lazy" alt="{label}: {name}"></a><figcaption>{label}</figcaption></figure>')
    parts.append('</div>')
for path in sorted((folder/'After').glob('*.png')):
    if path.name in names:
        continue
    parts.append(f'<h2>Comprobación adicional: {path.stem}</h2><figure><a href="After/{path.name}"><img src="After/{path.name}" loading="lazy" alt="{path.stem}"></a></figure>')
parts.append('</html>')
(folder/'index.html').write_text('\n'.join(parts),encoding='utf-8')
print('Before/after pairs:',len(names),folder/'index.html')
