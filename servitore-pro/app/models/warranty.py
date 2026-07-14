from app.extensions import db
from datetime import datetime


class Warranty(db.Model):
    __tablename__ = 'warranties'
    id = db.Column(db.Integer, primary_key=True)
    warranty_no = db.Column(db.String(50), unique=True, nullable=False)
    customer_id = db.Column(db.Integer, db.ForeignKey('customers.id'), nullable=False)
    item_id = db.Column(db.Integer, db.ForeignKey('items.id'), nullable=True)
    item_name = db.Column(db.String(150), nullable=True)  # free text if item not in master
    serial_no = db.Column(db.String(100), nullable=True)
    purchase_date = db.Column(db.Date, nullable=False)
    warranty_months = db.Column(db.Integer, default=12)
    expiry_date = db.Column(db.Date, nullable=False)
    vendor = db.Column(db.String(150), nullable=True)
    invoice_no = db.Column(db.String(100), nullable=True)
    notes = db.Column(db.Text, nullable=True)
    status = db.Column(db.String(20), default='Active')  # Active, Expired, Claimed
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    created_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)

    item = db.relationship('Item', foreign_keys=[item_id], lazy='select')
    creator = db.relationship('User', foreign_keys=[created_by], lazy='select')

    @property
    def is_expired(self):
        from datetime import date
        return self.expiry_date < date.today()

    @property
    def days_to_expiry(self):
        from datetime import date
        delta = self.expiry_date - date.today()
        return delta.days

    def __repr__(self):
        return f'<Warranty {self.warranty_no}>'
