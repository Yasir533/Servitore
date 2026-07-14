from flask import render_template, redirect, url_for, flash, request
from flask_login import login_required, current_user
from datetime import date, timedelta
from app.blueprints.warranty import warranty_bp
from app.extensions import db
from app.models.warranty import Warranty
from app.models.customer import Customer
from app.models.item import Item


def _generate_warranty_no():
    last = Warranty.query.order_by(Warranty.id.desc()).first()
    num = (last.id + 1) if last else 1
    return f'WR-{num:05d}'


@warranty_bp.route('/')
@login_required
def list_warranties():
    q = request.args.get('q', '')
    status_filter = request.args.get('status', '')
    near_expiry = request.args.get('near_expiry', '')
    query = Warranty.query.join(Customer)
    if q:
        query = query.filter(Customer.name.ilike(f'%{q}%') | Warranty.warranty_no.ilike(f'%{q}%'))
    if status_filter:
        query = query.filter(Warranty.status == status_filter)
    if near_expiry:
        threshold = date.today() + timedelta(days=30)
        query = query.filter(Warranty.expiry_date <= threshold, Warranty.expiry_date >= date.today())
    warranties = query.order_by(Warranty.expiry_date).all()
    return render_template('warranty/list.html', warranties=warranties, q=q,
                           status_filter=status_filter, near_expiry=near_expiry)


@warranty_bp.route('/add', methods=['GET', 'POST'])
@login_required
def add_warranty():
    if request.method == 'POST':
        purchase_date = date.fromisoformat(request.form.get('purchase_date'))
        months = int(request.form.get('warranty_months', 12))
        expiry = purchase_date + timedelta(days=30 * months)
        w = Warranty(
            warranty_no=_generate_warranty_no(),
            customer_id=request.form.get('customer_id'),
            item_id=request.form.get('item_id') or None,
            item_name=request.form.get('item_name', '').strip(),
            serial_no=request.form.get('serial_no', '').strip(),
            purchase_date=purchase_date,
            warranty_months=months,
            expiry_date=expiry,
            vendor=request.form.get('vendor', '').strip(),
            invoice_no=request.form.get('invoice_no', '').strip(),
            notes=request.form.get('notes', '').strip(),
            status='Active',
            created_by=current_user.id,
        )
        db.session.add(w)
        db.session.commit()
        flash(f'Warranty {w.warranty_no} added successfully.', 'success')
        return redirect(url_for('warranty.list_warranties'))
    customers = Customer.query.filter_by(is_active=True).order_by(Customer.name).all()
    items = Item.query.filter_by(is_active=True).order_by(Item.name).all()
    return render_template('warranty/form.html', warranty=None, customers=customers, items=items)


@warranty_bp.route('/<int:id>/edit', methods=['GET', 'POST'])
@login_required
def edit_warranty(id):
    w = Warranty.query.get_or_404(id)
    if request.method == 'POST':
        w.status = request.form.get('status', w.status)
        w.notes = request.form.get('notes', '').strip()
        db.session.commit()
        flash('Warranty updated.', 'success')
        return redirect(url_for('warranty.list_warranties'))
    customers = Customer.query.filter_by(is_active=True).order_by(Customer.name).all()
    items = Item.query.filter_by(is_active=True).order_by(Item.name).all()
    return render_template('warranty/form.html', warranty=w, customers=customers, items=items)
