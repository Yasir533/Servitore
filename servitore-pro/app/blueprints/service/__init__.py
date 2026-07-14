from flask import Blueprint
service_bp = Blueprint('service', __name__)
from app.blueprints.service import routes  # noqa
