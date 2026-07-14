from app.extensions import db
from datetime import datetime


class AreaMaster(db.Model):
    __tablename__ = 'area_masters'
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), unique=True, nullable=False)
    city = db.Column(db.String(100), nullable=True)
    state = db.Column(db.String(100), nullable=True)
    pincode = db.Column(db.String(10), nullable=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    customers = db.relationship('Customer', backref='area', lazy=True)

    def __repr__(self):
        return f'<Area {self.name}>'
