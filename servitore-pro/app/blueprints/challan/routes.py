from flask import render_template, redirect, url_for, flash, request
from flask_login import login_required, current_user
from datetime import date
from app.blueprints.challan import challan_bp
from app.extensions import db
from app.models.delivery_challan import DeliveryChallan, ChallanItem
from app.models.customer import Customer
from app.models.item import Item


def _generate_challan_no():
    last = DeliveryChallan.query.order_by(DeliveryChallan.id.desc()).first()
    num = (last.id + 1) if last else 1
    return f'DC-{date.today().strftime("%Y%m")}-{num:04d}'


@challan_bp.route('/')
@login_required
def list_challans():
    q = request.args.get('q', '')
    received_filter = request.args.get('received', '')
    query = DeliveryChallan.query.join(Customer)
    if q:
        query = query.filter(Customer.name.ilike(f'%{q}%') | DeliveryChallan.challan_no.ilike(f'%{q}%'))
    if received_filter == 'pending':
        query = query.filter(DeliveryChallan.is_received == False)
    elif received_filter == 'received':
        query = query.filter(DeliveryChallan.is_received == True)
    challans = query.order_by(DeliveryChallan.dispatch_date.desc()).all()
    return render_template('challan/list.html', challans=challans, q=q, received_filter=received_filter)


@challan_bp.route('/add', methods=['GET', 'POST'])
@login_required
def add_challan():
    if request.method == 'POST':
        challan = DeliveryChallan(
            challan_no=_generate_challan_no(),
            customer_id=request.form.get('customer_id'),
            dispatch_date=date.fromisoformat(request.form.get('dispatch_date', str(date.today()))),
            expected_return_date=date.fromisoformat(request.form.get('expected_return_date')) if request.form.get('expected_return_date') else None,
            notes=request.form.get('notes', '').strip(),
            created_by=current_user.id,
        )
        db.session.add(challan)
        db.session.flush()

        # Parse line items
        item_names = request.form.getlist('item_name[]')
        serial_nos = request.form.getlist('serial_no[]')
        quantities = request.form.getlist('quantity[]')
        conditions = request.form.getlist('condition[]')
        item_ids = request.form.getlist('item_id[]')

        for i, name in enumerate(item_names):
            if name.strip():
                ci = ChallanItem(
                    challan_id=challan.id,
                    item_id=item_ids[i] if item_ids[i] else None,
                    item_name=name.strip(),
                    serial_no=serial_nos[i] if i < len(serial_nos) else '',
                    quantity=int(quantities[i]) if i < len(quantities) and quantities[i] else 1,
                    condition=conditions[i] if i < len(conditions) else '',
                )
                db.session.add(ci)

        db.session.commit()
        flash(f'Delivery Challan {challan.challan_no} created.', 'success')
        return redirect(url_for('challan.list_challans'))
    customers = Customer.query.filter_by(is_active=True).order_by(Customer.name).all()
    items = Item.query.filter_by(is_active=True).order_by(Item.name).all()
    return render_template('challan/form.html', challan=None, customers=customers, items=items,
                           today=date.today().isoformat())


@challan_bp.route('/<int:id>/mark-received', methods=['POST'])
@login_required
def mark_received(id):
    challan = DeliveryChallan.query.get_or_404(id)
    challan.is_received = True
    challan.received_date = date.today()
    db.session.commit()
    flash(f'Challan {challan.challan_no} marked as received.', 'success')
    return redirect(url_for('challan.list_challans'))


@challan_bp.route('/<int:id>/print')
@login_required
def print_challan(id):
    challan = DeliveryChallan.query.get_or_404(id)
    return render_template('challan/print.html', challan=challan)
