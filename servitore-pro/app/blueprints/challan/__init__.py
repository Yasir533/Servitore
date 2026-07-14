from flask import Blueprint
challan_bp = Blueprint('challan', __name__)
from app.blueprints.challan import routes  # noqa
