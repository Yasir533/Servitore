from flask import render_template, redirect, url_for, flash, request
from flask_login import login_required, current_user
from datetime import date, datetime
from app.blueprints.service import service_bp
from app.extensions import db
from app.models.service_call import (ServiceCall, STATUS_UNASSIGNED, STATUS_CLOSED,
    STATUS_PENDING, STATUS_IN_PROGRESS, STATUS_TRANSFER,
    STATUS_PENDING_CUSTOMER, STATUS_PENDING_SPARE, STATUS_PENDING_TECHNICAL,
    STATUS_PENDING_OTHER, STATUS_STANDBY, ALL_STATUSES)
from app.models.customer import Customer
from app.models.item import Item
from app.models.categories import ServiceCategory
from app.models.area_master import AreaMaster
from app.models.asp import AuthorisedServiceProvider
from app.models.user import User


def _generate_call_no():
    last = ServiceCall.query.order_by(ServiceCall.id.desc()).first()
    num = (last.id + 1) if last else 1
    return f'SC-{date.today().strftime("%Y%m")}-{num:05d}'


@service_bp.route('/')
@service_bp.route('/pending')
@login_required
def pending_calls():
    status_filter = request.args.get('status', '')
    q = request.args.get('q', '')
    area_filter = request.args.get('area', '')
    engineer_filter = request.args.get('engineer', '')
    priority_only = request.args.get('priority', '')
    deadline_only = request.args.get('deadline', '')

    query = ServiceCall.query.filter(ServiceCall.status != STATUS_CLOSED)

    if status_filter:
        query = query.filter(ServiceCall.status == status_filter)
    if q:
        query = query.join(Customer).filter(
            Customer.name.ilike(f'%{q}%') | ServiceCall.call_no.ilike(f'%{q}%')
        )
    if area_filter:
        query = query.filter(ServiceCall.area_id == area_filter)
    if engineer_filter:
        query = query.filter(ServiceCall.engineer_id == engineer_filter)
    if priority_only:
        query = query.filter(ServiceCall.is_priority == True)
    if deadline_only:
        query = query.filter(ServiceCall.is_deadline == True)

    calls = query.order_by(ServiceCall.is_priority.desc(), ServiceCall.call_date).all()
    areas = AreaMaster.query.order_by(AreaMaster.name).all()
    engineers = User.query.filter(User.role.in_(['Engineer', 'Admin', 'Manager'])).all()
    return render_template('service/pending.html', calls=calls, statuses=ALL_STATUSES,
                           status_filter=status_filter, q=q, areas=areas,
                           engineers=engineers, area_filter=area_filter,
                           engineer_filter=engineer_filter,
                           priority_only=priority_only, deadline_only=deadline_only)


@service_bp.route('/register', methods=['GET', 'POST'])
@login_required
def register():
    if request.method == 'POST':
        sc = ServiceCall(
            call_no=_generate_call_no(),
            customer_id=request.form.get('customer_id'),
            item_id=request.form.get('item_id') or None,
            item_name=request.form.get('item_name', '').strip(),
            serial_no=request.form.get('serial_no', '').strip(),
            problem_description=request.form.get('problem_description', '').strip(),
            service_category_id=request.form.get('service_category_id') or None,
            area_id=request.form.get('area_id') or None,
            asp_id=request.form.get('asp_id') or None,
            engineer_id=request.form.get('engineer_id') or None,
            call_date=date.fromisoformat(request.form.get('call_date', str(date.today()))),
            schedule_date=date.fromisoformat(request.form.get('schedule_date')) if request.form.get('schedule_date') else None,
            deadline_date=date.fromisoformat(request.form.get('deadline_date')) if request.form.get('deadline_date') else None,
            is_priority=request.form.get('is_priority') == 'on',
            is_deadline=request.form.get('is_deadline') == 'on',
            spare_required=request.form.get('spare_required') == 'on',
            spare_details=request.form.get('spare_details', '').strip(),
            remarks=request.form.get('remarks', '').strip(),
            status=STATUS_UNASSIGNED if not request.form.get('engineer_id') else STATUS_PENDING,
            created_by=current_user.id,
        )
        db.session.add(sc)
        db.session.commit()
        flash(f'Service Call {sc.call_no} registered successfully.', 'success')
        return redirect(url_for('service.pending_calls'))
    customers = Customer.query.filter_by(is_active=True).order_by(Customer.name).all()
    items = Item.query.filter_by(is_active=True).order_by(Item.name).all()
    categories = ServiceCategory.query.order_by(ServiceCategory.name).all()
    areas = AreaMaster.query.order_by(AreaMaster.name).all()
    asps = AuthorisedServiceProvider.query.filter_by(is_active=True).order_by(AuthorisedServiceProvider.name).all()
    engineers = User.query.filter(User.role.in_(['Engineer', 'Admin', 'Manager'])).filter_by(is_active=True).all()
    return render_template('service/register.html',
                           customers=customers, items=items,
                           categories=categories, areas=areas,
                           asps=asps, engineers=engineers,
                           today=date.today().isoformat())


@service_bp.route('/<int:id>/update', methods=['GET', 'POST'])
@login_required
def update_call(id):
    sc = ServiceCall.query.get_or_404(id)
    if request.method == 'POST':
        sc.status = request.form.get('status', sc.status)
        sc.engineer_id = request.form.get('engineer_id') or sc.engineer_id
        sc.resolution = request.form.get('resolution', '').strip()
        sc.labour_charge = float(request.form.get('labour_charge', 0) or 0)
        sc.spare_charge = float(request.form.get('spare_charge', 0) or 0)
        sc.total_amount = sc.labour_charge + sc.spare_charge
        sc.remarks = request.form.get('remarks', '').strip()
        sc.is_priority = request.form.get('is_priority') == 'on'
        sc.is_deadline = request.form.get('is_deadline') == 'on'
        if sc.status == STATUS_CLOSED and not sc.closed_date:
            sc.closed_date = datetime.utcnow()
        db.session.commit()
        flash(f'Call {sc.call_no} updated.', 'success')
        return redirect(url_for('service.pending_calls'))
    engineers = User.query.filter(User.role.in_(['Engineer', 'Admin', 'Manager'])).filter_by(is_active=True).all()
    return render_template('service/update.html', call=sc, statuses=ALL_STATUSES, engineers=engineers)


@service_bp.route('/transfer-requested')
@login_required
def transfer_requested():
    calls = ServiceCall.query.filter_by(status=STATUS_TRANSFER).order_by(ServiceCall.call_date).all()
    return render_template('service/transfer.html', calls=calls)


@service_bp.route('/all')
@login_required
def all_calls():
    q = request.args.get('q', '')
    status_filter = request.args.get('status', '')
    date_from = request.args.get('date_from', '')
    date_to = request.args.get('date_to', '')
    query = ServiceCall.query
    if q:
        query = query.join(Customer).filter(
            Customer.name.ilike(f'%{q}%') | ServiceCall.call_no.ilike(f'%{q}%')
        )
    if status_filter:
        query = query.filter(ServiceCall.status == status_filter)
    if date_from:
        query = query.filter(ServiceCall.call_date >= date.fromisoformat(date_from))
    if date_to:
        query = query.filter(ServiceCall.call_date <= date.fromisoformat(date_to))
    calls = query.order_by(ServiceCall.call_date.desc()).all()
    return render_template('service/all_calls.html', calls=calls,
                           statuses=ALL_STATUSES, status_filter=status_filter,
                           q=q, date_from=date_from, date_to=date_to)
