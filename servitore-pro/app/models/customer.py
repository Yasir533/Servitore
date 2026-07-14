from app.extensions import db
from datetime import datetime


class Customer(db.Model):
    __tablename__ = 'customers'
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(150), nullable=False)
    type_id = db.Column(db.Integer, db.ForeignKey('customer_types.id'), nullable=True)
    area_id = db.Column(db.Integer, db.ForeignKey('area_masters.id'), nullable=True)
    phone = db.Column(db.String(20), nullable=True)
    mobile = db.Column(db.String(20), nullable=True)
    email = db.Column(db.String(120), nullable=True)
    address = db.Column(db.Text, nullable=True)
    city = db.Column(db.String(100), nullable=True)
    pincode = db.Column(db.String(10), nullable=True)
    gst_number = db.Column(db.String(20), nullable=True)
    is_active = db.Column(db.Boolean, default=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)

    service_calls = db.relationship('ServiceCall', backref='customer', lazy=True)
    warranties = db.relationship('Warranty', backref='customer', lazy=True)
    maintenance_contracts = db.relationship('MaintenanceContract', backref='customer', lazy=True)
    challans = db.relationship('DeliveryChallan', backref='customer', lazy=True)

    def __repr__(self):
        return f'<Customer {self.name}>'
