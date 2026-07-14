import os
from datetime import date, timedelta
from app import create_app
from app.extensions import db
from app.models.user import User
from app.models.customer_type import CustomerType
from app.models.area_master import AreaMaster
from app.models.customer import Customer
from app.models.categories import ServiceCategory, ItemCategory
from app.models.item import Manufacturer, Item
from app.models.asp import AuthorisedServiceProvider
from app.models.tax_master import TaxMaster
from app.models.service_center import ServiceCenter
from app.models.service_call import ServiceCall, STATUS_PENDING, STATUS_IN_PROGRESS, STATUS_CLOSED, STATUS_UNASSIGNED, STATUS_STANDBY, STATUS_PENDING_CUSTOMER, STATUS_PENDING_SPARE
from app.models.maintenance_contract import MaintenanceContract, PMCall
from app.models.warranty import Warranty

app = create_app()

with app.app_context():
    print("Seeding sample data...")
    
    # 1. Users
    if User.query.filter_by(username='engineer1').first() is None:
        eng1 = User(username='engineer1', email='eng1@saiservices.com', role='Engineer', full_name='Rohan Sharma', phone='9876543210')
        eng1.set_password('engineer123')
        db.session.add(eng1)
    else:
        eng1 = User.query.filter_by(username='engineer1').first()

    if User.query.filter_by(username='engineer2').first() is None:
        eng2 = User(username='engineer2', email='eng2@saiservices.com', role='Engineer', full_name='Amit Verma', phone='9876543211')
        eng2.set_password('engineer123')
        db.session.add(eng2)
    else:
        eng2 = User.query.filter_by(username='engineer2').first()

    if User.query.filter_by(username='manager1').first() is None:
        mgr = User(username='manager1', email='mgr@saiservices.com', role='Manager', full_name='Suresh Patel', phone='9876543212')
        mgr.set_password('manager123')
        db.session.add(mgr)
    
    admin_user = User.query.filter_by(username='admin').first()

    # 2. Customer Types
    ct_corporate = CustomerType.query.filter_by(name='Corporate').first() or CustomerType(name='Corporate', description='Business clients and offices')
    ct_retail = CustomerType.query.filter_by(name='Retail').first() or CustomerType(name='Retail', description='Individual customers')
    ct_gov = CustomerType.query.filter_by(name='Government').first() or CustomerType(name='Government', description='Public sector & schools')
    db.session.add_all([ct_corporate, ct_retail, ct_gov])
    db.session.flush()

    # 3. Areas
    a1 = AreaMaster.query.filter_by(name='Andheri West').first() or AreaMaster(name='Andheri West', city='Mumbai', state='Maharashtra', pincode='400053')
    a2 = AreaMaster.query.filter_by(name='Bandra Kurla Complex').first() or AreaMaster(name='Bandra Kurla Complex', city='Mumbai', state='Maharashtra', pincode='400051')
    a3 = AreaMaster.query.filter_by(name='Thane West').first() or AreaMaster(name='Thane West', city='Thane', state='Maharashtra', pincode='400601')
    db.session.add_all([a1, a2, a3])
    db.session.flush()

    # 4. Customers
    cust1 = Customer.query.filter_by(name='Apex Tech Solutions').first() or Customer(
        name='Apex Tech Solutions', customer_type=ct_corporate, area=a2, phone='022-26543210',
        mobile='9820012345', email='contact@apextech.com', address='Block G, BKC, Bandra', city='Mumbai', pincode='400051', gst_number='27AAAAA1111A1Z1'
    )
    cust2 = Customer.query.filter_by(name='Dr. Kishore Mehta').first() or Customer(
        name='Dr. Kishore Mehta', customer_type=ct_retail, area=a1, mobile='9819988776',
        email='kishore@mehta-clinic.in', address='102, Sunrise Towers, Lokhandwala', city='Mumbai', pincode='400053'
    )
    cust3 = Customer.query.filter_by(name='Municipal Corporation Office').first() or Customer(
        name='Municipal Corporation Office', customer_type=ct_gov, area=a3, phone='022-25432109',
        address='Near Station Road, Thane', city='Thane', pincode='400601'
    )
    db.session.add_all([cust1, cust2, cust3])
    db.session.flush()

    # 5. Service Categories & Item Categories
    sc_hardware = ServiceCategory.query.filter_by(name='Hardware Fault').first() or ServiceCategory(name='Hardware Fault', description='Physical damage or component failure')
    sc_software = ServiceCategory.query.filter_by(name='Software Install').first() or ServiceCategory(name='Software Install', description='OS or driver installation')
    sc_pm = ServiceCategory.query.filter_by(name='Preventive Maintenance').first() or ServiceCategory(name='Preventive Maintenance', description='Scheduled routine maintenance check')
    
    ic_printer = ItemCategory.query.filter_by(name='Laser Printers').first() or ItemCategory(name='Laser Printers', description='Laserjet & Inkjet printers')
    ic_ups = ItemCategory.query.filter_by(name='UPS Systems').first() or ItemCategory(name='UPS Systems', description='Uninterruptible power supplies')
    ic_pc = ItemCategory.query.filter_by(name='Laptops/Desktops').first() or ItemCategory(name='Laptops/Desktops', description='Personal computers and workstations')
    
    db.session.add_all([sc_hardware, sc_software, sc_pm, ic_printer, ic_ups, ic_pc])
    db.session.flush()

    # 6. Manufacturers & Items
    m_hp = Manufacturer.query.filter_by(name='HP').first() or Manufacturer(name='HP', contact='HP India Support')
    m_apc = Manufacturer.query.filter_by(name='APC').first() or Manufacturer(name='APC', contact='APC Schneider support')
    m_dell = Manufacturer.query.filter_by(name='Dell').first() or Manufacturer(name='Dell', contact='Dell Business support')
    db.session.add_all([m_hp, m_apc, m_dell])
    db.session.flush()

    item_lj = Item.query.filter_by(name='HP LaserJet Pro M404dn').first() or Item(
        name='HP LaserJet Pro M404dn', model_no='M404dn', serial_no_required=True,
        category=ic_printer, manufacturer=m_hp, unit_price=18500.00
    )
    item_ups = Item.query.filter_by(name='APC Back-UPS 600VA').first() or Item(
        name='APC Back-UPS 600VA', model_no='BX600C-IN', serial_no_required=True,
        category=ic_ups, manufacturer=m_apc, unit_price=3800.00
    )
    item_lat = Item.query.filter_by(name='Dell Latitude 3540').first() or Item(
        name='Dell Latitude 3540', model_no='L3540', serial_no_required=True,
        category=ic_pc, manufacturer=m_dell, unit_price=55000.00
    )
    db.session.add_all([item_lj, item_ups, item_lat])
    db.session.flush()

    # 7. ASP
    asp = AuthorisedServiceProvider.query.filter_by(name='TechnoCare Solutions').first() or AuthorisedServiceProvider(
        name='TechnoCare Solutions', contact_person='Vikram Joshi', phone='9812345678', email='support@technocare.com'
    )
    db.session.add(asp)
    db.session.flush()

    # 8. Service Calls
    today = date.today()
    
    # 1. Unassigned
    sc1 = ServiceCall(
        call_no='SC-202607-00001', customer=cust1, item=item_lj, item_name='HP LaserJet M404dn',
        serial_no='CNB123456', problem_description='Paper jam in fuser area. Error 75 showing on screen.',
        service_category=sc_hardware, area=a2, call_date=today - timedelta(days=2),
        status=STATUS_UNASSIGNED, is_priority=True
    )
    # 2. Assigned / Pending
    sc2 = ServiceCall(
        call_no='SC-202607-00002', customer=cust2, item=item_ups, item_name='APC Back-UPS 600VA',
        serial_no='APC987654', problem_description='UPS not turning on. Completely dead battery suspected.',
        service_category=sc_hardware, area=a1, engineer=eng1, call_date=today - timedelta(days=1),
        status=STATUS_PENDING, is_priority=False, schedule_date=today + timedelta(days=1)
    )
    # 3. In Progress
    sc3 = ServiceCall(
        call_no='SC-202607-00003', customer=cust3, item=item_lat, item_name='Dell Latitude 3540',
        serial_no='DLL54321', problem_description='Windows OS corrupted after update. Blue screen loop.',
        service_category=sc_software, area=a3, engineer=eng2, call_date=today,
        status=STATUS_IN_PROGRESS, is_priority=True, is_deadline=True, deadline_date=today
    )
    # 4. Closed (with billing)
    sc4 = ServiceCall(
        call_no='SC-202607-00004', customer=cust1, item=item_lj, item_name='HP LaserJet M404dn',
        serial_no='CNB112233', problem_description='Toner cartridge replacement and calibration.',
        service_category=sc_hardware, area=a2, engineer=eng1, call_date=today - timedelta(days=4),
        status=STATUS_CLOSED, closed_date=today - timedelta(days=1), resolution='Replaced toner cartridge with OEM HP 76A. Performed alignment calibration test pages OK.',
        labour_charge=500.00, spare_charge=6500.00, total_amount=7000.00, bill_no='B-2607-101'
    )
    # 5. Pending for Spare
    sc5 = ServiceCall(
        call_no='SC-202607-00005', customer=cust2, item=item_lj, item_name='Laser printer',
        problem_description='Need new paper tray. Existing one broken.',
        service_category=sc_hardware, area=a1, engineer=eng1, call_date=today - timedelta(days=3),
        status=STATUS_PENDING_SPARE, spare_required=True, spare_details='Paper feed tray for HP Laserjet'
    )
    db.session.add_all([sc1, sc2, sc3, sc4, sc5])

    # 9. Warranties
    w1 = Warranty(
        warranty_no='WR-00001', customer=cust1, item=item_lat, serial_no='DLL54321',
        purchase_date=today - timedelta(days=200), warranty_months=12, expiry_date=today + timedelta(days=165),
        vendor='Sai Support Systems', status='Active'
    )
    w2 = Warranty(
        warranty_no='WR-00002', customer=cust2, item=item_ups, serial_no='APC987654',
        purchase_date=today - timedelta(days=350), warranty_months=12, expiry_date=today + timedelta(days=15),
        vendor='Sagar Electronics', status='Active'
    )
    db.session.add_all([w1, w2])

    # 10. Maintenance Contracts & PM Calls
    mc1 = MaintenanceContract(
        contract_no='MC-00001', customer=cust1, start_date=today - timedelta(days=30),
        end_date=today + timedelta(days=335), amount=15000.00, pm_frequency_months=3,
        description='Comprehensive AMC for LaserJet printers and network equipment.',
        status='Active', creator=admin_user
    )
    db.session.add(mc1)
    db.session.flush()

    # Add Scheduled PM Calls
    pm1 = PMCall(contract_id=mc1.id, scheduled_date=today + timedelta(days=60), status='Pending')
    pm2 = PMCall(contract_id=mc1.id, scheduled_date=today + timedelta(days=150), status='Pending')
    pm3 = PMCall(contract_id=mc1.id, scheduled_date=today - timedelta(days=5), status='Pending')  # Overdue PM Call
    db.session.add_all([pm1, pm2, pm3])

    db.session.commit()
    print("Database successfully seeded with realistic sample data!")
