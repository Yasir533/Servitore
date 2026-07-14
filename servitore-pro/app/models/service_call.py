from app.extensions import db
from datetime import datetime


# Status constants
STATUS_PENDING = 'Pending'
STATUS_IN_PROGRESS = 'In Progress'
STATUS_CLOSED = 'Closed'
STATUS_TRANSFER = 'Transfer Requested'
STATUS_PENDING_CUSTOMER = 'Pending - Customer'
STATUS_PENDING_SPARE = 'Pending - Spare'
STATUS_PENDING_TECHNICAL = 'Pending - Technical'
STATUS_PENDING_OTHER = 'Pending - Others'
STATUS_UNASSIGNED = 'Unassigned'
STATUS_STANDBY = 'StandBy'

ALL_STATUSES = [
    STATUS_PENDING, STATUS_IN_PROGRESS, STATUS_CLOSED,
    STATUS_TRANSFER, STATUS_PENDING_CUSTOMER, STATUS_PENDING_SPARE,
    STATUS_PENDING_TECHNICAL, STATUS_PENDING_OTHER, STATUS_UNASSIGNED, STATUS_STANDBY
]


class ServiceCall(db.Model):
    __tablename__ = 'service_calls'
    id = db.Column(db.Integer, primary_key=True)
    call_no = db.Column(db.String(50), unique=True, nullable=False)
    customer_id = db.Column(db.Integer, db.ForeignKey('customers.id'), nullable=False)
    item_id = db.Column(db.Integer, db.ForeignKey('items.id'), nullable=True)
    item_name = db.Column(db.String(150), nullable=True)  # free text
    serial_no = db.Column(db.String(100), nullable=True)
    problem_description = db.Column(db.Text, nullable=False)
    service_category_id = db.Column(db.Integer, db.ForeignKey('service_categories.id'), nullable=True)
    area_id = db.Column(db.Integer, db.ForeignKey('area_masters.id'), nullable=True)
    asp_id = db.Column(db.Integer, db.ForeignKey('asps.id'), nullable=True)
    engineer_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    call_date = db.Column(db.Date, nullable=False, default=datetime.utcnow)
    schedule_date = db.Column(db.Date, nullable=True)
    deadline_date = db.Column(db.Date, nullable=True)
    closed_date = db.Column(db.DateTime, nullable=True)
    status = db.Column(db.String(50), default=STATUS_UNASSIGNED)
    is_priority = db.Column(db.Boolean, default=False)
    is_deadline = db.Column(db.Boolean, default=False)
    resolution = db.Column(db.Text, nullable=True)
    spare_required = db.Column(db.Boolean, default=False)
    spare_details = db.Column(db.String(255), nullable=True)
    labour_charge = db.Column(db.Float, default=0.0)
    spare_charge = db.Column(db.Float, default=0.0)
    total_amount = db.Column(db.Float, default=0.0)
    bill_no = db.Column(db.String(50), nullable=True)
    remarks = db.Column(db.Text, nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    created_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)

    item = db.relationship('Item', foreign_keys=[item_id], lazy='select')
    service_category = db.relationship('ServiceCategory', foreign_keys=[service_category_id], lazy='select')
    area = db.relationship('AreaMaster', foreign_keys=[area_id], lazy='select')
    asp = db.relationship('AuthorisedServiceProvider', foreign_keys=[asp_id], lazy='select')
    engineer = db.relationship('User', foreign_keys=[engineer_id], lazy='select')
    creator = db.relationship('User', foreign_keys=[created_by], lazy='select')

    @property
    def is_open(self):
        return self.status != STATUS_CLOSED

    def __repr__(self):
        return f'<ServiceCall {self.call_no}>'
