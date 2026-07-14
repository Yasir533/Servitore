from flask import render_template, jsonify
from flask_login import login_required, current_user
from datetime import date
from app.blueprints.dashboard import dashboard_bp
from app.models.service_call import (
    ServiceCall, STATUS_PENDING, STATUS_IN_PROGRESS, STATUS_TRANSFER,
    STATUS_PENDING_CUSTOMER, STATUS_PENDING_SPARE, STATUS_PENDING_TECHNICAL,
    STATUS_PENDING_OTHER, STATUS_UNASSIGNED, STATUS_STANDBY, STATUS_CLOSED
)
from app.models.delivery_challan import DeliveryChallan
from app.models.maintenance_contract import PMCall


def get_quick_view_stats():
    today = date.today()
    all_open = ServiceCall.query.filter(ServiceCall.status != STATUS_CLOSED)

    pending = all_open.filter(ServiceCall.status == STATUS_PENDING).count()
    in_progress = all_open.filter(ServiceCall.status == STATUS_IN_PROGRESS).count()
    deadline = all_open.filter(ServiceCall.is_deadline == True).count()
    priority = all_open.filter(ServiceCall.is_priority == True).count()
    registered_today = ServiceCall.query.filter(
        ServiceCall.call_date == today
    ).count()
    closed_today = ServiceCall.query.filter(
        ServiceCall.closed_date != None,
        ServiceCall.closed_date >= today
    ).count()
    pend_customer = all_open.filter(ServiceCall.status == STATUS_PENDING_CUSTOMER).count()
    pend_spare = all_open.filter(ServiceCall.status == STATUS_PENDING_SPARE).count()
    pend_technical = all_open.filter(ServiceCall.status == STATUS_PENDING_TECHNICAL).count()
    pend_other = all_open.filter(ServiceCall.status == STATUS_PENDING_OTHER).count()
    unassigned = all_open.filter(ServiceCall.status == STATUS_UNASSIGNED).count()
    transfer = all_open.filter(ServiceCall.status == STATUS_TRANSFER).count()
    standby = all_open.filter(ServiceCall.status == STATUS_STANDBY).count()
    challan_pending = DeliveryChallan.query.filter_by(is_received=False).count()
    pm_pending = PMCall.query.filter_by(status='Pending').count()

    return {
        'pending': pending,
        'in_progress': in_progress,
        'deadline': deadline,
        'priority': priority,
        'registered_today': registered_today,
        'closed_today': closed_today,
        'pend_customer': pend_customer,
        'pend_spare': pend_spare,
        'pend_technical': pend_technical,
        'pend_other': pend_other,
        'unassigned': unassigned,
        'transfer': transfer,
        'standby': standby,
        'challan_pending': challan_pending,
        'pm_pending': pm_pending,
    }


@dashboard_bp.route('/')
@dashboard_bp.route('/dashboard')
@login_required
def index():
    stats = get_quick_view_stats()
    # Recent service calls
    recent_calls = ServiceCall.query.order_by(ServiceCall.created_at.desc()).limit(10).all()
    # Upcoming PM calls
    pm_calls = PMCall.query.filter_by(status='Pending').order_by(PMCall.scheduled_date).limit(5).all()
    return render_template('dashboard/index.html',
                           stats=stats, recent_calls=recent_calls, pm_calls=pm_calls)


@dashboard_bp.route('/api/quick-view')
@login_required
def api_quick_view():
    return jsonify(get_quick_view_stats())
