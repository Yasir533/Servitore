from app.extensions import db
from datetime import datetime


class MaintenanceContract(db.Model):
    __tablename__ = 'maintenance_contracts'
    id = db.Column(db.Integer, primary_key=True)
    contract_no = db.Column(db.String(50), unique=True, nullable=False)
    customer_id = db.Column(db.Integer, db.ForeignKey('customers.id'), nullable=False)
    start_date = db.Column(db.Date, nullable=False)
    end_date = db.Column(db.Date, nullable=False)
    amount = db.Column(db.Float, default=0.0)
    pm_frequency_months = db.Column(db.Integer, default=3)  # PM every N months
    description = db.Column(db.Text, nullable=True)
    status = db.Column(db.String(20), default='Active')  # Active, Expired, Cancelled
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    created_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)

    pm_calls = db.relationship('PMCall', backref='contract', lazy=True)
    creator = db.relationship('User', foreign_keys=[created_by], lazy='select')

    @property
    def is_expired(self):
        from datetime import date
        return self.end_date < date.today()

    def __repr__(self):
        return f'<Contract {self.contract_no}>'


class PMCall(db.Model):
    __tablename__ = 'pm_calls'
    id = db.Column(db.Integer, primary_key=True)
    contract_id = db.Column(db.Integer, db.ForeignKey('maintenance_contracts.id'), nullable=False)
    scheduled_date = db.Column(db.Date, nullable=False)
    completed_date = db.Column(db.Date, nullable=True)
    engineer_id = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    status = db.Column(db.String(20), default='Pending')  # Pending, Completed
    notes = db.Column(db.Text, nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    engineer = db.relationship('User', foreign_keys=[engineer_id], lazy='select')
