from app.extensions import db
from datetime import datetime


class ServiceCenter(db.Model):
    __tablename__ = 'service_centers'
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(150), nullable=False)
    contact_person = db.Column(db.String(100), nullable=True)
    phone = db.Column(db.String(20), nullable=True)
    email = db.Column(db.String(120), nullable=True)
    address = db.Column(db.Text, nullable=True)
    area_id = db.Column(db.Integer, db.ForeignKey('area_masters.id'), nullable=True)
    is_active = db.Column(db.Boolean, default=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    area = db.relationship('AreaMaster', lazy='select')

    def __repr__(self):
        return f'<ServiceCenter {self.name}>'
