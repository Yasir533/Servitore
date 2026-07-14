from app.extensions import db
from datetime import datetime


class CustomerType(db.Model):
    __tablename__ = 'customer_types'
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), unique=True, nullable=False)
    description = db.Column(db.String(255), nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    customers = db.relationship('Customer', backref='customer_type', lazy=True)

    def __repr__(self):
        return f'<CustomerType {self.name}>'
