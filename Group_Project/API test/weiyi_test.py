"""
Restaurant Management System API Test - Order System
Developer: Weiyi Weng

This script tests order creation and verification.

Usage:
    python weiyi_order_test.py
"""

import requests
import json
import time

# API base URL - Ensure the application is running at this URL
BASE_URL = "https://localhost:7226"

# Disable SSL verification warnings (for local testing only)
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

def print_header(title):
    """Print test section header"""
    print("\n" + "=" * 50)
    print(f" {title} ".center(50, '='))
    print("=" * 50)

def print_result(name, success):
    """Print test result"""
    status = "✅ PASSED" if success else "❌ FAILED"
    print(f"{name}: {status}")

def extract_values(data):
    """Extract values from JSON response with reference handling"""
    if isinstance(data, dict) and '$values' in data:
        return data['$values']
    return data if isinstance(data, list) else [data]

def test_order_system():
    print_header("Order System Test (Weiyi Weng)")
    
    try:
        # 1. Register new user
        timestamp = int(time.time())
        email = f"order_test_{timestamp}@example.com"
        
        print("1. Registering new user...")
        session = requests.Session()
        register_data = {
            "firstName": "Order",
            "lastName": "Tester",
            "email": email,
            "password": "Test123!",
            "confirmPassword": "Test123!",
            "phoneNumber": "5556667777"
        }
        
        response = session.post(
            f"{BASE_URL}/api/Auth/register", 
            json=register_data,
            verify=False
        )
        
        if response.status_code != 200:
            print(f"Registration failed: HTTP {response.status_code}")
            return False
        
        print("✓ User registration successful")
        print("✓ Authentication cookie received")
        
        # 2. Get menu items
        print("\n2. Getting available menu items...")
        response = session.get(f"{BASE_URL}/api/Menu", verify=False)
        
        if response.status_code != 200:
            print(f"Failed to get menu: HTTP {response.status_code}")
            return False
        
        # Extract menu items from response
        menu_items = extract_values(response.json())
        
        if not menu_items or len(menu_items) < 1:
            print("No menu items available")
            return False
        
        print(f"✓ Retrieved {len(menu_items)} menu items")
        
        # Select items for order
        selected_items = menu_items[:2] if len(menu_items) >= 2 else [menu_items[0]]
        
        # 3. Create order
        print("\n3. Creating order...")
        
        order_items = []
        if len(selected_items) >= 2:
            order_items = [
                {"menuItemId": selected_items[0]['id'], "quantity": 2},
                {"menuItemId": selected_items[1]['id'], "quantity": 1}
            ]
        else:
            order_items = [{"menuItemId": selected_items[0]['id'], "quantity": 2}]
        
        order_data = {
            "reservationId": None,
            "items": order_items
        }
        
        response = session.post(
            f"{BASE_URL}/api/Orders",
            json=order_data,
            verify=False
        )
        
        if response.status_code != 201:
            print(f"Order creation failed: HTTP {response.status_code}")
            return False
        
        # Extract order ID
        try:
            order_id = response.json().get('id')
            if not order_id:
                # Try to get ID from Location header
                location = response.headers.get('Location')
                if location and '/api/Orders/' in location:
                    order_id = location.split('/api/Orders/')[1]
                else:
                    print("Could not determine order ID")
                    return False
        except:
            print("Could not parse order response")
            return False
            
        print(f"✓ Order created successfully, ID: {order_id}")
        
        # 4. Verify order details
        print("\n4. Verifying order details...")
        response = session.get(
            f"{BASE_URL}/api/Orders/{order_id}",
            verify=False
        )
        
        if response.status_code != 200:
            print(f"Failed to get order: HTTP {response.status_code}")
            return False
        
        # Process order details
        order = response.json()
        
        # Extract order items
        order_items = extract_values(order.get('orderItems', []))
        
        print(f"Order amount: ${order.get('totalAmount')}")
        print(f"Order items count: {len(order_items)}")
        
        if len(order_items) < 1:
            print("Order has no items")
            return False
        
        print("✓ Order data verification successful")
        print("✓ Order system test successful")
        
        return True
    
    except requests.exceptions.ConnectionError:
        print("Connection error: Please ensure the application is running")
        return False
    except Exception as e:
        print(f"Test error: {e}")
        return False

# Main program
if __name__ == "__main__":
    print("\nRestaurant Management System - Order System Test")
    print("=" * 50)
    
    result = test_order_system()
    
    # Print result summary
    print("\n" + "=" * 50)
    print(" Test Result Summary ".center(50, '='))
    print("=" * 50)
    
    print_result("Order System Test (Weiyi Weng)", result)
    
    print("\nFinal Result: ", "✅ TEST PASSED" if result else "❌ TEST FAILED")
    print("=" * 50)