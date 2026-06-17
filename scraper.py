import urllib.request
import re
import json

req = urllib.request.Request('https://en.wikipedia.org/wiki/List_of_RAL_colors', headers={'User-Agent': 'Mozilla/5.0'})
html = urllib.request.urlopen(req).read().decode('utf-8')

pattern = re.compile(r'<th>\s*(?:<a[^>]*>)?(RAL\s+\d{4})(?:</a>)?\s*</th>\s*<td[^>]*>(?:<a[^>]*>)?([^<]+?)(?:</a>)?</td>.*?background-color:\s*(#[0-9A-Fa-f]{6})', re.IGNORECASE | re.DOTALL)
matches = pattern.findall(html)

colors = []
for code, name, hexval in matches:
    colors.append({'Code': code.strip(), 'Name': name.strip(), 'Hex': hexval.strip()})

unique = {c['Code']: c for c in colors}
with open('ral.json', 'w', encoding='utf-8') as f:
    json.dump(list(unique.values()), f, indent=2)

print(f'Extracted {len(unique)} colors')
