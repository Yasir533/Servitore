import os, secrets
from flask import Flask, render_template, session, request, abort, jsonify
from app.config import config_by_name
from app.extensions import db, migrate, login_manager


def create_app(config_name=None):
    app = Flask(__name__, instance_relative_config=True)
    if not config_name:
        config_name = os.environ.get('FLASK_ENV', 'development')
    app.config.from_object(config_by_name.get(config_name, config_by_name['default']))

    try:
        os.makedirs(app.instance_path)
    except OSError:
        pass

    # Extensions
    db.init_app(app)
    migrate.init_app(app, db)
    login_manager.init_app(app)
    login_manager.login_view = 'auth.login'
    login_manager.login_message = 'Please log in to access this page.'
    login_manager.login_message_category = 'warning'

    @login_manager.user_loader
    def load_user(user_id):
        from app.models.user import User
        return db.session.get(User, int(user_id))

    # Blueprints
    from app.blueprints.auth import auth_bp
    from app.blueprints.dashboard import dashboard_bp
    from app.blueprints.masters import masters_bp
    from app.blueprints.maintenance import maintenance_bp
    from app.blueprints.warranty import warranty_bp
    from app.blueprints.service import service_bp
    from app.blueprints.challan import challan_bp
    from app.blueprints.reports import reports_bp
    from app.blueprints.administration import admin_bp

    app.register_blueprint(auth_bp, url_prefix='/auth')
    app.register_blueprint(dashboard_bp, url_prefix='/')
    app.register_blueprint(masters_bp, url_prefix='/masters')
    app.register_blueprint(maintenance_bp, url_prefix='/maintenance')
    app.register_blueprint(warranty_bp, url_prefix='/warranty')
    app.register_blueprint(service_bp, url_prefix='/service')
    app.register_blueprint(challan_bp, url_prefix='/challan')
    app.register_blueprint(reports_bp, url_prefix='/reports')
    app.register_blueprint(admin_bp, url_prefix='/administration')

    # Health Check API for health monitors
    @app.route('/api/auth/ping')
    def ping():
        return jsonify(status="healthy", message="pong"), 200

    # CSRF Protection
    def generate_csrf_token():
        if 'csrf_token' not in session:
            session['csrf_token'] = secrets.token_hex(32)
        return session['csrf_token']

    @app.context_processor
    def inject_csrf():
        return dict(csrf_token=generate_csrf_token)

    @app.before_request
    def check_csrf():
        if request.method in ['POST', 'PUT', 'DELETE', 'PATCH']:
            if request.path.startswith('/api'):
                return
            token = request.form.get('csrf_token') or request.headers.get('X-CSRF-Token')
            expected = session.get('csrf_token')
            if not expected or not token or not secrets.compare_digest(expected, token):
                abort(400, 'CSRF token missing or invalid')

    # Create all tables
    with app.app_context():
        db.create_all()
        _seed_admin(app)

    # Error handlers
    @app.errorhandler(403)
    def forbidden(e):
        return render_template('errors/403.html'), 403

    @app.errorhandler(404)
    def not_found(e):
        return render_template('errors/404.html'), 404

    @app.errorhandler(500)
    def server_error(e):
        return render_template('errors/500.html'), 500

    return app


def _seed_admin(app):
    """Create default admin user if none exists."""
    from app.models.user import User
    with app.app_context():
        if User.query.count() == 0:
            admin = User(username='admin', email='admin@saiservices.com', role='Admin', full_name='Administrator')
            admin.set_password('admin123')
            db.session.add(admin)
            db.session.commit()
