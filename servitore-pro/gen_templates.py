"""Generate all remaining masters templates."""
import os

BASE = r'c:\Users\moham\.gemini\antigravity-ide\scratch\servitore-pro\app\templates'

MASTERS = [
    {
        'folder': 'area',
        'title': 'Area Master',
        'icon': 'bi-map',
        'list_route': 'masters.areas',
        'add_route': 'masters.add_area',
        'edit_route': 'masters.edit_area',
        'cols': [('Name', 'name'), ('City', 'city'), ('State', 'state'), ('Pincode', 'pincode')],
        'form_fields': [
            ('Name', 'name', 'text', True),
            ('City', 'city', 'text', False),
            ('State', 'state', 'text', False),
            ('Pincode', 'pincode', 'text', False),
        ]
    },
    {
        'folder': 'asp',
        'title': 'Auth. Service Provider',
        'icon': 'bi-building',
        'list_route': 'masters.asps',
        'add_route': 'masters.add_asp',
        'edit_route': 'masters.edit_asp',
        'cols': [('Name', 'name'), ('Contact', 'contact_person'), ('Phone', 'phone'), ('City', 'city')],
        'form_fields': [
            ('Name', 'name', 'text', True),
            ('Contact Person', 'contact_person', 'text', False),
            ('Phone', 'phone', 'text', False),
            ('Email', 'email', 'email', False),
            ('City', 'city', 'text', False),
            ('GST Number', 'gst_number', 'text', False),
            ('Address', 'address', 'textarea', False),
        ]
    },
    {
        'folder': 'tax',
        'title': 'Tax Master',
        'icon': 'bi-percent',
        'list_route': 'masters.taxes',
        'add_route': 'masters.add_tax',
        'edit_route': 'masters.edit_tax',
        'cols': [('Name', 'name'), ('Percentage (%)', 'percentage'), ('Description', 'description')],
        'form_fields': [
            ('Name', 'name', 'text', True),
            ('Percentage (%)', 'percentage', 'number', True),
            ('Description', 'description', 'text', False),
        ]
    },
    {
        'folder': 'service_category',
        'title': 'Service Category',
        'icon': 'bi-collection',
        'list_route': 'masters.service_categories',
        'add_route': 'masters.add_service_category',
        'edit_route': 'masters.edit_service_category',
        'cols': [('Name', 'name'), ('Description', 'description')],
        'form_fields': [
            ('Name', 'name', 'text', True),
            ('Description', 'description', 'text', False),
        ]
    },
    {
        'folder': 'item_category',
        'title': 'Item Category',
        'icon': 'bi-grid',
        'list_route': 'masters.item_categories',
        'add_route': 'masters.add_item_category',
        'edit_route': 'masters.edit_item_category',
        'cols': [('Name', 'name'), ('Description', 'description')],
        'form_fields': [
            ('Name', 'name', 'text', True),
            ('Description', 'description', 'text', False),
        ]
    },
    {
        'folder': 'manufacturer',
        'title': 'Manufacturer',
        'icon': 'bi-gear',
        'list_route': 'masters.manufacturers',
        'add_route': 'masters.add_manufacturer',
        'edit_route': 'masters.edit_manufacturer',
        'cols': [('Name', 'name'), ('Contact', 'contact'), ('Phone', 'phone'), ('Email', 'email')],
        'form_fields': [
            ('Name', 'name', 'text', True),
            ('Contact Person', 'contact', 'text', False),
            ('Phone', 'phone', 'text', False),
            ('Email', 'email', 'email', False),
            ('Address', 'address', 'textarea', False),
        ]
    },
    {
        'folder': 'service_center',
        'title': 'Service Center',
        'icon': 'bi-shop',
        'list_route': 'masters.service_centers',
        'add_route': 'masters.add_service_center',
        'edit_route': 'masters.edit_service_center',
        'cols': [('Name', 'name'), ('Contact', 'contact_person'), ('Phone', 'phone')],
        'form_fields': [
            ('Name', 'name', 'text', True),
            ('Contact Person', 'contact_person', 'text', False),
            ('Phone', 'phone', 'text', False),
            ('Email', 'email', 'email', False),
            ('Address', 'address', 'textarea', False),
        ]
    },
    {
        'folder': 'item',
        'title': 'Item',
        'icon': 'bi-box',
        'list_route': 'masters.items',
        'add_route': 'masters.add_item',
        'edit_route': 'masters.edit_item',
        'cols': [('Name', 'name'), ('Model No', 'model_no'), ('Category', 'category_id'), ('Price', 'unit_price'), ('Status', 'is_active')],
        'form_fields': []  # custom
    },
]


def make_list(m):
    cols_html = ''.join(f'<th>{c[0]}</th>' for c in m['cols']) + '<th>Action</th>'
    rows_html = ''
    for c in m['cols']:
        f = c[1]
        rows_html += f'<td style="font-size:12.5px;">{{{{ it.{f} if it.{f} else \'—\' }}}}</td>\n          '
    return f'''{{%  extends 'layouts/base.html' %}}
{{% block title %}}{m['title']}s{{% endblock %}}
{{% block topbar_title %}}Masters — {m['title']}s{{% endblock %}}
{{% block content %}}
<div class="page-header">
  <div><div class="page-title">{m['title']}s</div></div>
  <a href="{{{{ url_for('{m['add_route']}') }}}}" class="btn btn-primary"><i class="bi bi-plus-lg"></i> Add</a>
</div>
<div class="card">
  <div class="card-header"><i class="{m['icon']} text-accent"></i><span class="card-header-title">{m['title']}s</span></div>
  <div class="table-responsive">
    {{%  if items %}}
    <table class="data-table"><thead><tr>{cols_html}</tr></thead>
    <tbody>{{%  for it in items %}}<tr>
      {rows_html}
      <td><a href="{{{{ url_for('{m['edit_route']}', id=it.id) }}}}" class="btn btn-sm btn-secondary"><i class="bi bi-pencil-square"></i></a></td>
    </tr>{{%  endfor %}}</tbody></table>
    {{%  else %}}
    <div class="empty-state"><i class="{m['icon']}"></i><h3>No {m['title'].lower()}s found</h3></div>
    {{%  endif %}}
  </div>
</div>
{{% endblock %}}'''


def make_form(m):
    fields_html = ''
    for label, fname, ftype, req in m.get('form_fields', []):
        req_mark = '<span class="required">*</span>' if req else ''
        req_attr = 'required' if req else ''
        if ftype == 'textarea':
            fields_html += f'<div class="form-group full"><label class="form-label">{label}</label><textarea name="{fname}" class="form-control" rows="2">{{{{ item.{fname} if item else \'\' }}}}</textarea></div>\n        '
        elif ftype == 'number':
            fields_html += f'<div class="form-group"><label class="form-label">{label} {req_mark}</label><input type="number" name="{fname}" class="form-control" step="0.01" value="{{{{ item.{fname} if item else \'\' }}}}" {req_attr}></div>\n        '
        else:
            fields_html += f'<div class="form-group"><label class="form-label">{label} {req_mark}</label><input type="{ftype}" name="{fname}" class="form-control" value="{{{{ item.{fname} if item else \'\' }}}}" {req_attr}></div>\n        '
    return f'''{{%  extends 'layouts/base.html' %}}
{{% block title %}}{{{{ 'Edit' if item else 'Add' }}}} {m['title']}{{% endblock %}}
{{% block topbar_title %}}{{{{ 'Edit' if item else 'Add' }}}} {m['title']}{{% endblock %}}
{{% block content %}}
<div class="page-header"><div><div class="page-title">{{{{ 'Edit' if item else 'Add' }}}} {m['title']}</div></div><a href="{{{{ url_for('{m['list_route']}') }}}}" class="btn btn-secondary"><i class="bi bi-arrow-left"></i> Back</a></div>
<div class="card" style="max-width:600px;">
  <div class="card-header"><i class="{m['icon']} text-accent"></i><span class="card-header-title">{m['title']} Details</span></div>
  <div class="card-body">
    <form method="POST" action="{{{{ url_for('{m['edit_route']}', id=item.id) if item else url_for('{m['add_route']}') }}}}">
      <input type="hidden" name="csrf_token" value="{{{{ csrf_token() }}}}">
      <div class="form-grid">
        {fields_html}
      </div>
      <div style="display:flex;gap:10px;margin-top:16px;"><button type="submit" class="btn btn-primary"><i class="bi bi-check-lg"></i> {{{{ 'Update' if item else 'Add' }}}}</button><a href="{{{{ url_for('{m['list_route']}') }}}}" class="btn btn-secondary">Cancel</a></div>
    </form>
  </div>
</div>
{{% endblock %}}'''


# Item is special
ITEM_LIST = '''{% extends 'layouts/base.html' %}
{% block title %}Items{% endblock %}
{% block topbar_title %}Masters — Items{% endblock %}
{% block content %}
<div class="page-header">
  <div><div class="page-title">Items</div><div class="page-subtitle">Products and equipment catalogue</div></div>
  <a href="{{ url_for('masters.add_item') }}" class="btn btn-primary"><i class="bi bi-plus-lg"></i> Add Item</a>
</div>
<div class="filters-bar">
  <form method="GET" style="display:contents;">
    <div class="filter-group"><div class="filter-label">Search</div><input type="text" name="q" class="filter-control" placeholder="Item name" value="{{ q }}"></div>
    <button type="submit" class="btn btn-primary btn-sm" style="align-self:flex-end;"><i class="bi bi-search"></i></button>
    <a href="{{ url_for('masters.items') }}" class="btn btn-secondary btn-sm" style="align-self:flex-end;">Clear</a>
  </form>
</div>
<div class="card">
  <div class="card-header"><i class="bi bi-box text-accent"></i><span class="card-header-title">Items</span><span style="margin-left:8px;font-size:12px;color:var(--text-muted);">({{ items|length }})</span></div>
  <div class="table-responsive">
    {% if items %}
    <table class="data-table">
      <thead><tr><th>Name</th><th>Model No</th><th>Category</th><th>Manufacturer</th><th>Unit Price</th><th>Status</th><th>Action</th></tr></thead>
      <tbody>
        {% for it in items %}
        <tr>
          <td class="fw-600">{{ it.name }}</td>
          <td style="font-size:12.5px;color:var(--text-muted);">{{ it.model_no or '—' }}</td>
          <td style="font-size:12.5px;">{{ it.category.name if it.category else '—' }}</td>
          <td style="font-size:12.5px;">{{ it.manufacturer.name if it.manufacturer else '—' }}</td>
          <td style="font-size:12.5px;color:var(--status-closed);">{{ '₹%.2f'|format(it.unit_price) if it.unit_price else '—' }}</td>
          <td><span class="badge badge-{{ 'active' if it.is_active else 'expired' }}">{{ 'Active' if it.is_active else 'Inactive' }}</span></td>
          <td><a href="{{ url_for('masters.edit_item', id=it.id) }}" class="btn btn-sm btn-secondary"><i class="bi bi-pencil-square"></i></a></td>
        </tr>
        {% endfor %}
      </tbody>
    </table>
    {% else %}
    <div class="empty-state"><i class="bi bi-box"></i><h3>No items found</h3></div>
    {% endif %}
  </div>
</div>
{% endblock %}
'''

ITEM_FORM = '''{% extends 'layouts/base.html' %}
{% block title %}{{ 'Edit' if item else 'Add' }} Item{% endblock %}
{% block topbar_title %}{{ 'Edit' if item else 'Add' }} Item{% endblock %}
{% block content %}
<div class="page-header"><div><div class="page-title">{{ 'Edit' if item else 'Add' }} Item</div></div><a href="{{ url_for('masters.items') }}" class="btn btn-secondary"><i class="bi bi-arrow-left"></i> Back</a></div>
<div class="card" style="max-width:600px;">
  <div class="card-header"><i class="bi bi-box text-accent"></i><span class="card-header-title">Item Details</span></div>
  <div class="card-body">
    <form method="POST" action="{{ url_for('masters.edit_item', id=item.id) if item else url_for('masters.add_item') }}">
      <input type="hidden" name="csrf_token" value="{{ csrf_token() }}">
      <div class="form-grid">
        <div class="form-group full"><label class="form-label">Item Name <span class="required">*</span></label><input type="text" name="name" class="form-control" value="{{ item.name if item else '' }}" required></div>
        <div class="form-group"><label class="form-label">Model Number</label><input type="text" name="model_no" class="form-control" value="{{ item.model_no if item else '' }}"></div>
        <div class="form-group"><label class="form-label">Category</label>
          <select name="category_id" class="form-control"><option value="">-- None --</option>{% for c in categories %}<option value="{{ c.id }}" {% if item and item.category_id == c.id %}selected{% endif %}>{{ c.name }}</option>{% endfor %}</select>
        </div>
        <div class="form-group"><label class="form-label">Manufacturer</label>
          <select name="manufacturer_id" class="form-control"><option value="">-- None --</option>{% for m in manufacturers %}<option value="{{ m.id }}" {% if item and item.manufacturer_id == m.id %}selected{% endif %}>{{ m.name }}</option>{% endfor %}</select>
        </div>
        <div class="form-group"><label class="form-label">Unit Price (Rs.)</label><input type="number" name="unit_price" class="form-control" step="0.01" value="{{ item.unit_price if item else '0' }}"></div>
        <div class="form-group" style="justify-content:flex-end;padding-top:20px;">
          <div style="display:flex;flex-direction:column;gap:8px;">
            <label class="form-check"><input type="checkbox" name="serial_no_required" class="form-check-input" {% if item and item.serial_no_required %}checked{% endif %}><span class="form-check-label">Serial No Required</span></label>
            {% if item %}<label class="form-check"><input type="checkbox" name="is_active" class="form-check-input" {% if item.is_active %}checked{% endif %}><span class="form-check-label">Active</span></label>{% endif %}
          </div>
        </div>
      </div>
      <div style="display:flex;gap:10px;margin-top:16px;"><button type="submit" class="btn btn-primary"><i class="bi bi-check-lg"></i> {{ 'Update' if item else 'Add Item' }}</button><a href="{{ url_for('masters.items') }}" class="btn btn-secondary">Cancel</a></div>
    </form>
  </div>
</div>
{% endblock %}
'''

for m in MASTERS:
    folder = os.path.join(BASE, 'masters', m['folder'])
    os.makedirs(folder, exist_ok=True)

    if m['folder'] == 'item':
        with open(os.path.join(folder, 'list.html'), 'w', encoding='utf-8') as f:
            f.write(ITEM_LIST)
        with open(os.path.join(folder, 'form.html'), 'w', encoding='utf-8') as f:
            f.write(ITEM_FORM)
        print(f"Created item templates")
        continue

    list_content = make_list(m)
    form_content = make_form(m)

    with open(os.path.join(folder, 'list.html'), 'w', encoding='utf-8') as f:
        f.write(list_content)
    with open(os.path.join(folder, 'form.html'), 'w', encoding='utf-8') as f:
        f.write(form_content)
    print(f"Created {m['folder']} templates")

print("All masters templates generated.")
