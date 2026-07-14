from app.extensions import db
from datetime import datetime


class DeliveryChallan(db.Model):
    __tablename__ = 'delivery_challans'
    id = db.Column(db.Integer, primary_key=True)
    challan_no = db.Column(db.String(50), unique=True, nullable=False)
    customer_id = db.Column(db.Integer, db.ForeignKey('customers.id'), nullable=False)
    dispatch_date = db.Column(db.Date, nullable=False, default=datetime.utcnow)
    expected_return_date = db.Column(db.Date, nullable=True)
    received_date = db.Column(db.Date, nullable=True)
    is_received = db.Column(db.Boolean, default=False)
    notes = db.Column(db.Text, nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    created_by = db.Column(db.Integer, db.ForeignKey('users.id'), nullable=True)
    creator = db.relationship('User', foreign_keys=[created_by], lazy='select')

    items = db.relationship('ChallanItem', backref='challan', lazy=True, cascade='all, delete-orphan')

    def __repr__(self):
        return f'<DeliveryChallan {self.challan_no}>'


class ChallanItem(db.Model):
    __tablename__ = 'challan_items'
    id = db.Column(db.Integer, primary_key=True)
    challan_id = db.Column(db.Integer, db.ForeignKey('delivery_challans.id'), nullable=False)
    item_id = db.Column(db.Integer, db.ForeignKey('items.id'), nullable=True)
    item_name = db.Column(db.String(150), nullable=False)
    serial_no = db.Column(db.String(100), nullable=True)
    quantity = db.Column(db.Integer, default=1)
    condition = db.Column(db.String(100), nullable=True)  # Good, Damaged, etc.

    item = db.relationship('Item', foreign_keys=[item_id], lazy='select')
