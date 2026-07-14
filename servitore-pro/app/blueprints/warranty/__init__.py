from flask import Blueprint
warranty_bp = Blueprint('warranty', __name__)
from app.blueprints.warranty import routes  # noqa
