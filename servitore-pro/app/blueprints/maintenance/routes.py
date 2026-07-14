from flask import render_template, redirect, url_for, flash, request
from flask_login import login_required, current_user
from datetime import date, timedelta
from app.blueprints.maintenance import maintenance_bp
from app.extensions import db
from app.models.maintenance_contract import MaintenanceContract, PMCall
from app.models.customer import Customer
from app.models.user import User


def _generate_contract_no():
    last = MaintenanceContract.query.order_by(MaintenanceContract.id.desc()).first()
    num = (last.id + 1) if last else 1
    return f'MC-{num:05d}'


@maintenance_bp.route('/')
@login_required
def list_contracts():
    status_filter = request.args.get('status', '')
    q = request.args.get('q', '')
    query = MaintenanceContract.query.join(Customer)
    if status_filter:
        query = query.filter(MaintenanceContract.status == status_filter)
    if q:
        query = query.filter(Customer.name.ilike(f'%{q}%'))
    contracts = query.order_by(MaintenanceContract.end_date.desc()).all()
    return render_template('maintenance/list.html', contracts=contracts,
                           status_filter=status_filter, q=q)


@maintenance_bp.route('/new', methods=['GET', 'POST'])
@login_required
def new_contract():
    if request.method == 'POST':
        start = date.fromisoformat(request.form.get('start_date'))
        end = date.fromisoformat(request.form.get('end_date'))
        contract = MaintenanceContract(
            contract_no=_generate_contract_no(),
            customer_id=request.form.get('customer_id'),
            start_date=start,
            end_date=end,
            amount=float(request.form.get('amount', 0) or 0),
            pm_frequency_months=int(request.form.get('pm_frequency_months', 3)),
            description=request.form.get('description', '').strip(),
            status='Active',
            created_by=current_user.id,
        )
        db.session.add(contract)
        db.session.flush()

        # Auto-generate PM calls based on frequency
        freq = contract.pm_frequency_months
        current_date = start + timedelta(days=30 * freq)
        while current_date <= end:
            db.session.add(PMCall(
                contract_id=contract.id,
                scheduled_date=current_date,
                status='Pending'
            ))
            current_date = current_date + timedelta(days=30 * freq)

        db.session.commit()
        flash(f'Contract {contract.contract_no} created successfully.', 'success')
        return redirect(url_for('maintenance.list_contracts'))
    customers = Customer.query.filter_by(is_active=True).order_by(Customer.name).all()
    return render_template('maintenance/form.html', contract=None, customers=customers)


@maintenance_bp.route('/<int:id>')
@login_required
def view_contract(id):
    contract = MaintenanceContract.query.get_or_404(id)
    return render_template('maintenance/view.html', contract=contract)


@maintenance_bp.route('/pm-calls-pending')
@login_required
def pm_calls_pending():
    today = date.today()
    pm_calls = (PMCall.query
                .filter_by(status='Pending')
                .filter(PMCall.scheduled_date <= today)
                .order_by(PMCall.scheduled_date)
                .all())
    return render_template('maintenance/pm_calls.html', pm_calls=pm_calls)


@maintenance_bp.route('/pm-calls/<int:id>/complete', methods=['POST'])
@login_required
def complete_pm_call(id):
    pm = PMCall.query.get_or_404(id)
    pm.status = 'Completed'
    pm.completed_date = date.today()
    pm.engineer_id = current_user.id
    pm.notes = request.form.get('notes', '')
    db.session.commit()
    flash('PM Call marked as completed.', 'success')
    return redirect(url_for('maintenance.pm_calls_pending'))
