from app.extensions import db
from datetime import datetime


class TaxMaster(db.Model):
    __tablename__ = 'tax_masters'
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), nullable=False)
    percentage = db.Column(db.Float, nullable=False, default=0.0)
    description = db.Column(db.String(255), nullable=True)
    is_active = db.Column(db.Boolean, default=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)

    def __repr__(self):
        return f'<Tax {self.name} {self.percentage}%>'
