from flask import render_template, redirect, url_for, flash, request
from flask_login import login_required
from app.blueprints.masters import masters_bp
from app.extensions import db
from app.models.customer_type import CustomerType
from app.models.customer import Customer
from app.models.asp import AuthorisedServiceProvider
from app.models.tax_master import TaxMaster
from app.models.categories import ServiceCategory, ItemCategory
from app.models.item import Manufacturer, Item
from app.models.area_master import AreaMaster
from app.models.service_center import ServiceCenter


# ─── Customer Type ────────────────────────────────────────────────────────────
@masters_bp.route('/customer-types')
@login_required
def customer_types():
    items = CustomerType.query.order_by(CustomerType.name).all()
    return render_template('masters/customer_type/list.html', items=items)

@masters_bp.route('/customer-types/add', methods=['GET', 'POST'])
@login_required
def add_customer_type():
    if request.method == 'POST':
        name = request.form.get('name', '').strip()
        desc = request.form.get('description', '').strip()
        if not name:
            flash('Name is required.', 'danger')
        elif CustomerType.query.filter_by(name=name).first():
            flash('Customer type already exists.', 'danger')
        else:
            db.session.add(CustomerType(name=name, description=desc))
            db.session.commit()
            flash(f'Customer Type "{name}" added.', 'success')
            return redirect(url_for('masters.customer_types'))
    return render_template('masters/customer_type/form.html', item=None)

@masters_bp.route('/customer-types/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_customer_type(id):
    ct = CustomerType.query.get_or_404(id)
    if request.method == 'POST':
        ct.name = request.form.get('name', ct.name).strip()
        ct.description = request.form.get('description', '').strip()
        db.session.commit()
        flash('Customer Type updated.', 'success')
        return redirect(url_for('masters.customer_types'))
    return render_template('masters/customer_type/form.html', item=ct)

@masters_bp.route('/customer-types/<int:id>/delete', methods=['POST'])
@login_required
def delete_customer_type(id):
    ct = CustomerType.query.get_or_404(id)
    db.session.delete(ct)
    db.session.commit()
    flash('Customer Type deleted.', 'success')
    return redirect(url_for('masters.customer_types'))


# ─── Customer ─────────────────────────────────────────────────────────────────
@masters_bp.route('/customers')
@login_required
def customers():
    q = request.args.get('q', '')
    query = Customer.query
    if q:
        query = query.filter(Customer.name.ilike(f'%{q}%'))
    items = query.order_by(Customer.name).all()
    return render_template('masters/customer/list.html', items=items, q=q)

@masters_bp.route('/customers/add', methods=['GET', 'POST'])
@login_required
def add_customer():
    if request.method == 'POST':
        c = Customer(
            name=request.form.get('name', '').strip(),
            type_id=request.form.get('type_id') or None,
            area_id=request.form.get('area_id') or None,
            phone=request.form.get('phone', '').strip(),
            mobile=request.form.get('mobile', '').strip(),
            email=request.form.get('email', '').strip(),
            address=request.form.get('address', '').strip(),
            city=request.form.get('city', '').strip(),
            pincode=request.form.get('pincode', '').strip(),
            gst_number=request.form.get('gst_number', '').strip(),
        )
        db.session.add(c)
        db.session.commit()
        flash(f'Customer "{c.name}" added.', 'success')
        return redirect(url_for('masters.customers'))
    types = CustomerType.query.order_by(CustomerType.name).all()
    areas = AreaMaster.query.order_by(AreaMaster.name).all()
    return render_template('masters/customer/form.html', item=None, types=types, areas=areas)

@masters_bp.route('/customers/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_customer(id):
    c = Customer.query.get_or_404(id)
    if request.method == 'POST':
        c.name = request.form.get('name', c.name).strip()
        c.type_id = request.form.get('type_id') or None
        c.area_id = request.form.get('area_id') or None
        c.phone = request.form.get('phone', '').strip()
        c.mobile = request.form.get('mobile', '').strip()
        c.email = request.form.get('email', '').strip()
        c.address = request.form.get('address', '').strip()
        c.city = request.form.get('city', '').strip()
        c.pincode = request.form.get('pincode', '').strip()
        c.gst_number = request.form.get('gst_number', '').strip()
        c.is_active = request.form.get('is_active') == 'on'
        db.session.commit()
        flash('Customer updated.', 'success')
        return redirect(url_for('masters.customers'))
    types = CustomerType.query.order_by(CustomerType.name).all()
    areas = AreaMaster.query.order_by(AreaMaster.name).all()
    return render_template('masters/customer/form.html', item=c, types=types, areas=areas)


# ─── Area Master ──────────────────────────────────────────────────────────────
@masters_bp.route('/areas')
@login_required
def areas():
    items = AreaMaster.query.order_by(AreaMaster.name).all()
    return render_template('masters/area/list.html', items=items)

@masters_bp.route('/areas/add', methods=['GET', 'POST'])
@login_required
def add_area():
    if request.method == 'POST':
        a = AreaMaster(
            name=request.form.get('name', '').strip(),
            city=request.form.get('city', '').strip(),
            state=request.form.get('state', '').strip(),
            pincode=request.form.get('pincode', '').strip(),
        )
        db.session.add(a)
        db.session.commit()
        flash(f'Area "{a.name}" added.', 'success')
        return redirect(url_for('masters.areas'))
    return render_template('masters/area/form.html', item=None)

@masters_bp.route('/areas/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_area(id):
    a = AreaMaster.query.get_or_404(id)
    if request.method == 'POST':
        a.name = request.form.get('name', a.name).strip()
        a.city = request.form.get('city', '').strip()
        a.state = request.form.get('state', '').strip()
        a.pincode = request.form.get('pincode', '').strip()
        db.session.commit()
        flash('Area updated.', 'success')
        return redirect(url_for('masters.areas'))
    return render_template('masters/area/form.html', item=a)


# ─── ASP ──────────────────────────────────────────────────────────────────────
@masters_bp.route('/asps')
@login_required
def asps():
    items = AuthorisedServiceProvider.query.order_by(AuthorisedServiceProvider.name).all()
    return render_template('masters/asp/list.html', items=items)

@masters_bp.route('/asps/add', methods=['GET', 'POST'])
@login_required
def add_asp():
    if request.method == 'POST':
        a = AuthorisedServiceProvider(
            name=request.form.get('name', '').strip(),
            contact_person=request.form.get('contact_person', '').strip(),
            phone=request.form.get('phone', '').strip(),
            email=request.form.get('email', '').strip(),
            address=request.form.get('address', '').strip(),
            city=request.form.get('city', '').strip(),
            gst_number=request.form.get('gst_number', '').strip(),
        )
        db.session.add(a)
        db.session.commit()
        flash(f'ASP "{a.name}" added.', 'success')
        return redirect(url_for('masters.asps'))
    return render_template('masters/asp/form.html', item=None)

@masters_bp.route('/asps/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_asp(id):
    a = AuthorisedServiceProvider.query.get_or_404(id)
    if request.method == 'POST':
        a.name = request.form.get('name', a.name).strip()
        a.contact_person = request.form.get('contact_person', '').strip()
        a.phone = request.form.get('phone', '').strip()
        a.email = request.form.get('email', '').strip()
        a.address = request.form.get('address', '').strip()
        a.city = request.form.get('city', '').strip()
        a.gst_number = request.form.get('gst_number', '').strip()
        db.session.commit()
        flash('ASP updated.', 'success')
        return redirect(url_for('masters.asps'))
    return render_template('masters/asp/form.html', item=a)


# ─── Tax Master ───────────────────────────────────────────────────────────────
@masters_bp.route('/taxes')
@login_required
def taxes():
    items = TaxMaster.query.order_by(TaxMaster.name).all()
    return render_template('masters/tax/list.html', items=items)

@masters_bp.route('/taxes/add', methods=['GET', 'POST'])
@login_required
def add_tax():
    if request.method == 'POST':
        t = TaxMaster(
            name=request.form.get('name', '').strip(),
            percentage=float(request.form.get('percentage', 0)),
            description=request.form.get('description', '').strip(),
        )
        db.session.add(t)
        db.session.commit()
        flash(f'Tax "{t.name}" added.', 'success')
        return redirect(url_for('masters.taxes'))
    return render_template('masters/tax/form.html', item=None)

@masters_bp.route('/taxes/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_tax(id):
    t = TaxMaster.query.get_or_404(id)
    if request.method == 'POST':
        t.name = request.form.get('name', t.name).strip()
        t.percentage = float(request.form.get('percentage', t.percentage))
        t.description = request.form.get('description', '').strip()
        db.session.commit()
        flash('Tax updated.', 'success')
        return redirect(url_for('masters.taxes'))
    return render_template('masters/tax/form.html', item=t)


# ─── Service Category ─────────────────────────────────────────────────────────
@masters_bp.route('/service-categories')
@login_required
def service_categories():
    items = ServiceCategory.query.order_by(ServiceCategory.name).all()
    return render_template('masters/service_category/list.html', items=items)

@masters_bp.route('/service-categories/add', methods=['GET', 'POST'])
@login_required
def add_service_category():
    if request.method == 'POST':
        sc = ServiceCategory(
            name=request.form.get('name', '').strip(),
            description=request.form.get('description', '').strip(),
        )
        db.session.add(sc)
        db.session.commit()
        flash(f'Service Category "{sc.name}" added.', 'success')
        return redirect(url_for('masters.service_categories'))
    return render_template('masters/service_category/form.html', item=None)

@masters_bp.route('/service-categories/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_service_category(id):
    sc = ServiceCategory.query.get_or_404(id)
    if request.method == 'POST':
        sc.name = request.form.get('name', sc.name).strip()
        sc.description = request.form.get('description', '').strip()
        db.session.commit()
        flash('Service Category updated.', 'success')
        return redirect(url_for('masters.service_categories'))
    return render_template('masters/service_category/form.html', item=sc)


# ─── Item Category ────────────────────────────────────────────────────────────
@masters_bp.route('/item-categories')
@login_required
def item_categories():
    items = ItemCategory.query.order_by(ItemCategory.name).all()
    return render_template('masters/item_category/list.html', items=items)

@masters_bp.route('/item-categories/add', methods=['GET', 'POST'])
@login_required
def add_item_category():
    if request.method == 'POST':
        ic = ItemCategory(
            name=request.form.get('name', '').strip(),
            description=request.form.get('description', '').strip(),
        )
        db.session.add(ic)
        db.session.commit()
        flash(f'Item Category "{ic.name}" added.', 'success')
        return redirect(url_for('masters.item_categories'))
    return render_template('masters/item_category/form.html', item=None)

@masters_bp.route('/item-categories/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_item_category(id):
    ic = ItemCategory.query.get_or_404(id)
    if request.method == 'POST':
        ic.name = request.form.get('name', ic.name).strip()
        ic.description = request.form.get('description', '').strip()
        db.session.commit()
        flash('Item Category updated.', 'success')
        return redirect(url_for('masters.item_categories'))
    return render_template('masters/item_category/form.html', item=ic)


# ─── Manufacturer ─────────────────────────────────────────────────────────────
@masters_bp.route('/manufacturers')
@login_required
def manufacturers():
    items = Manufacturer.query.order_by(Manufacturer.name).all()
    return render_template('masters/manufacturer/list.html', items=items)

@masters_bp.route('/manufacturers/add', methods=['GET', 'POST'])
@login_required
def add_manufacturer():
    if request.method == 'POST':
        m = Manufacturer(
            name=request.form.get('name', '').strip(),
            contact=request.form.get('contact', '').strip(),
            phone=request.form.get('phone', '').strip(),
            email=request.form.get('email', '').strip(),
            address=request.form.get('address', '').strip(),
        )
        db.session.add(m)
        db.session.commit()
        flash(f'Manufacturer "{m.name}" added.', 'success')
        return redirect(url_for('masters.manufacturers'))
    return render_template('masters/manufacturer/form.html', item=None)

@masters_bp.route('/manufacturers/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_manufacturer(id):
    m = Manufacturer.query.get_or_404(id)
    if request.method == 'POST':
        m.name = request.form.get('name', m.name).strip()
        m.contact = request.form.get('contact', '').strip()
        m.phone = request.form.get('phone', '').strip()
        m.email = request.form.get('email', '').strip()
        m.address = request.form.get('address', '').strip()
        db.session.commit()
        flash('Manufacturer updated.', 'success')
        return redirect(url_for('masters.manufacturers'))
    return render_template('masters/manufacturer/form.html', item=m)


# ─── Item ─────────────────────────────────────────────────────────────────────
@masters_bp.route('/items')
@login_required
def items():
    q = request.args.get('q', '')
    query = Item.query
    if q:
        query = query.filter(Item.name.ilike(f'%{q}%'))
    all_items = query.order_by(Item.name).all()
    return render_template('masters/item/list.html', items=all_items, q=q)

@masters_bp.route('/items/add', methods=['GET', 'POST'])
@login_required
def add_item():
    if request.method == 'POST':
        it = Item(
            name=request.form.get('name', '').strip(),
            model_no=request.form.get('model_no', '').strip(),
            category_id=request.form.get('category_id') or None,
            manufacturer_id=request.form.get('manufacturer_id') or None,
            unit_price=float(request.form.get('unit_price', 0) or 0),
            serial_no_required=request.form.get('serial_no_required') == 'on',
        )
        db.session.add(it)
        db.session.commit()
        flash(f'Item "{it.name}" added.', 'success')
        return redirect(url_for('masters.items'))
    cats = ItemCategory.query.order_by(ItemCategory.name).all()
    mfrs = Manufacturer.query.order_by(Manufacturer.name).all()
    return render_template('masters/item/form.html', item=None, categories=cats, manufacturers=mfrs)

@masters_bp.route('/items/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_item(id):
    it = Item.query.get_or_404(id)
    if request.method == 'POST':
        it.name = request.form.get('name', it.name).strip()
        it.model_no = request.form.get('model_no', '').strip()
        it.category_id = request.form.get('category_id') or None
        it.manufacturer_id = request.form.get('manufacturer_id') or None
        it.unit_price = float(request.form.get('unit_price', 0) or 0)
        it.serial_no_required = request.form.get('serial_no_required') == 'on'
        it.is_active = request.form.get('is_active') == 'on'
        db.session.commit()
        flash('Item updated.', 'success')
        return redirect(url_for('masters.items'))
    cats = ItemCategory.query.order_by(ItemCategory.name).all()
    mfrs = Manufacturer.query.order_by(Manufacturer.name).all()
    return render_template('masters/item/form.html', item=it, categories=cats, manufacturers=mfrs)


# ─── Service Center ───────────────────────────────────────────────────────────
@masters_bp.route('/service-centers')
@login_required
def service_centers():
    items = ServiceCenter.query.order_by(ServiceCenter.name).all()
    return render_template('masters/service_center/list.html', items=items)

@masters_bp.route('/service-centers/add', methods=['GET', 'POST'])
@login_required
def add_service_center():
    if request.method == 'POST':
        sc = ServiceCenter(
            name=request.form.get('name', '').strip(),
            contact_person=request.form.get('contact_person', '').strip(),
            phone=request.form.get('phone', '').strip(),
            email=request.form.get('email', '').strip(),
            address=request.form.get('address', '').strip(),
            area_id=request.form.get('area_id') or None,
        )
        db.session.add(sc)
        db.session.commit()
        flash(f'Service Center "{sc.name}" added.', 'success')
        return redirect(url_for('masters.service_centers'))
    areas = AreaMaster.query.order_by(AreaMaster.name).all()
    return render_template('masters/service_center/form.html', item=None, areas=areas)

@masters_bp.route('/service-centers/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_service_center(id):
    sc = ServiceCenter.query.get_or_404(id)
    if request.method == 'POST':
        sc.name = request.form.get('name', sc.name).strip()
        sc.contact_person = request.form.get('contact_person', '').strip()
        sc.phone = request.form.get('phone', '').strip()
        sc.email = request.form.get('email', '').strip()
        sc.address = request.form.get('address', '').strip()
        sc.area_id = request.form.get('area_id') or None
        sc.is_active = request.form.get('is_active') == 'on'
        db.session.commit()
        flash('Service Center updated.', 'success')
        return redirect(url_for('masters.service_centers'))
    areas = AreaMaster.query.order_by(AreaMaster.name).all()
    return render_template('masters/service_center/form.html', item=sc, areas=areas)
