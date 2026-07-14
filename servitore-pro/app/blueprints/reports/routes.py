from flask import render_template, request, make_response
from flask_login import login_required
from datetime import date, timedelta
from sqlalchemy import func
from app.blueprints.reports import reports_bp
from app.models.service_call import ServiceCall, STATUS_CLOSED
from app.models.delivery_challan import DeliveryChallan
from app.models.customer import Customer
from app.models.area_master import AreaMaster
from app.models.asp import AuthorisedServiceProvider
from app.models.user import User
from app.models.warranty import Warranty
from app.extensions import db


def _date_range():
    date_from = request.args.get('date_from', (date.today() - timedelta(days=30)).isoformat())
    date_to = request.args.get('date_to', date.today().isoformat())
    return date.fromisoformat(date_from), date.fromisoformat(date_to), date_from, date_to


@reports_bp.route('/')
@login_required
def index():
    return render_template('reports/index.html')


@reports_bp.route('/service-bills')
@login_required
def service_bills():
    df, dt, date_from, date_to = _date_range()
    calls = (ServiceCall.query
             .filter(ServiceCall.status == STATUS_CLOSED)
             .filter(ServiceCall.closed_date >= df)
             .filter(ServiceCall.closed_date <= dt)
             .order_by(ServiceCall.closed_date.desc())
             .all())
    total = sum(c.total_amount for c in calls)
    return render_template('reports/service_bills.html', calls=calls, total=total,
                           date_from=date_from, date_to=date_to)


@reports_bp.route('/display-calls')
@login_required
def display_calls():
    df, dt, date_from, date_to = _date_range()
    status_filter = request.args.get('status', '')
    query = (ServiceCall.query
             .filter(ServiceCall.call_date >= df)
             .filter(ServiceCall.call_date <= dt))
    if status_filter:
        query = query.filter(ServiceCall.status == status_filter)
    calls = query.order_by(ServiceCall.call_date.desc()).all()
    return render_template('reports/display_calls.html', calls=calls,
                           date_from=date_from, date_to=date_to, status_filter=status_filter)


@reports_bp.route('/asp-reports')
@login_required
def asp_reports():
    df, dt, date_from, date_to = _date_range()
    # Company wise: group calls by ASP
    asp_data = []
    for asp in AuthorisedServiceProvider.query.all():
        calls = ServiceCall.query.filter_by(asp_id=asp.id).filter(
            ServiceCall.call_date >= df, ServiceCall.call_date <= dt
        ).all()
        if calls:
            asp_data.append({
                'asp': asp,
                'total': len(calls),
                'closed': sum(1 for c in calls if c.status == STATUS_CLOSED),
                'revenue': sum(c.total_amount for c in calls if c.status == STATUS_CLOSED),
            })
    return render_template('reports/asp_reports.html', asp_data=asp_data,
                           date_from=date_from, date_to=date_to)


@reports_bp.route('/area-reports')
@login_required
def area_reports():
    df, dt, date_from, date_to = _date_range()
    report_type = request.args.get('type', 'area_wise')
    areas = AreaMaster.query.order_by(AreaMaster.name).all()
    customers = Customer.query.order_by(Customer.name).all()
    engineers = User.query.filter(User.role.in_(['Engineer', 'Admin', 'Manager'])).all()

    data = []
    for area in areas:
        area_calls = ServiceCall.query.filter_by(area_id=area.id).filter(
            ServiceCall.call_date >= df, ServiceCall.call_date <= dt
        )
        calls = area_calls.all()
        if calls or report_type == 'area_wise':
            data.append({
                'area': area,
                'total': len(calls),
                'closed': sum(1 for c in calls if c.status == STATUS_CLOSED),
                'pending': sum(1 for c in calls if c.status != STATUS_CLOSED),
                'revenue': sum(c.total_amount for c in calls if c.status == STATUS_CLOSED),
                'bills_not_raised': sum(1 for c in calls if c.status == STATUS_CLOSED and c.total_amount == 0),
                'payment_pending': sum(c.total_amount for c in calls if c.status == STATUS_CLOSED),
            })
    return render_template('reports/area_reports.html', data=data, report_type=report_type,
                           date_from=date_from, date_to=date_to)


@reports_bp.route('/engineer-reports')
@login_required
def engineer_reports():
    df, dt, date_from, date_to = _date_range()
    engineers = User.query.filter(User.role.in_(['Engineer', 'Admin', 'Manager'])).all()
    data = []
    for eng in engineers:
        calls = ServiceCall.query.filter_by(engineer_id=eng.id).filter(
            ServiceCall.call_date >= df, ServiceCall.call_date <= dt
        ).all()
        data.append({
            'engineer': eng,
            'total': len(calls),
            'pending': sum(1 for c in calls if c.status != STATUS_CLOSED),
            'closed': sum(1 for c in calls if c.status == STATUS_CLOSED),
            'revenue': sum(c.total_amount for c in calls if c.status == STATUS_CLOSED),
            'expense': sum(c.spare_charge for c in calls),
        })
    return render_template('reports/engineer_reports.html', data=data,
                           date_from=date_from, date_to=date_to)


@reports_bp.route('/warranty-reports')
@login_required
def warranty_reports():
    report_type = request.args.get('type', 'expenditure')
    if report_type == 'near_expiry':
        threshold = date.today() + timedelta(days=30)
        warranties = Warranty.query.filter(
            Warranty.expiry_date <= threshold,
            Warranty.expiry_date >= date.today(),
            Warranty.status == 'Active'
        ).order_by(Warranty.expiry_date).all()
    else:
        df, dt, date_from, date_to = _date_range()
        warranties = Warranty.query.filter(
            Warranty.purchase_date >= df, Warranty.purchase_date <= dt
        ).order_by(Warranty.expiry_date).all()
    return render_template('reports/warranty_reports.html', warranties=warranties, report_type=report_type)


@reports_bp.route('/standby-reports')
@login_required
def standby_reports():
    from app.models.service_call import STATUS_STANDBY
    calls = ServiceCall.query.filter_by(status=STATUS_STANDBY).order_by(ServiceCall.call_date).all()
    return render_template('reports/standby_reports.html', calls=calls)
