"""
Restaurant Management System API Test - Menu Management
Developer: Junting Ren

This script tests menu management functionality, including creating and verifying menu items.

Usage:
    python junting_menu_test.py
"""

import requests
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

def test_menu_management():
    print_header("Menu Management Test (Junting Ren)")
    
    try:
        # 1. Admin login
        print("1. Logging in as admin...")
        
        admin_session = requests.Session()
        login_data = {
            "email": "admin@restaurant.com",
            "password": "Admin123!",
            "rememberMe": False
        }
        
        response = admin_session.post(
            f"{BASE_URL}/api/Auth/login",
            json=login_data,
            verify=False
        )
        
        if response.status_code != 200:
            print(f"Admin login failed: HTTP {response.status_code}")
            print(f"Response: {response.text}")
            return False
        
        print("✓ Admin login successful")
        print("✓ Authentication cookie received")
        
        # 2. Create a new menu item
        print("\n2. Creating a new menu item...")
        
        timestamp = int(time.time())
        menu_item = {
            "name": f"Test Item {timestamp}",
            "description": "Created by API test",
            "price": 12.99,
            "category": "Test",
            "imageUrl": "/images/menu/default.jpg",
            "isAvailable": True
        }
        
        response = admin_session.post(
            f"{BASE_URL}/api/Menu",
            json=menu_item,
            verify=False
        )
        
        if response.status_code != 201:
            print(f"Menu item creation failed: HTTP {response.status_code}")
            print(f"Response: {response.text}")
            return False
        
        # Get the ID of the newly created menu item
        new_item = response.json()
        item_id = new_item.get("id")
        
        if not item_id:
            print("Could not get menu item ID from response")
            return False
            
        print(f"✓ Menu item created successfully, ID: {item_id}")
        
        # 3. Verify the menu item
        print("\n3. Verifying menu item...")
        
        response = admin_session.get(
            f"{BASE_URL}/api/Menu/{item_id}",
            verify=False
        )
        
        if response.status_code != 200:
            print(f"Failed to retrieve menu item: HTTP {response.status_code}")
            print(f"Response: {response.text}")
            return False
        
        item = response.json()
        print(f"Retrieved item: {item.get('name')}, Price: ${item.get('price')}")
        
        # Verify item data matches what we created
        if item.get('name') == menu_item['name'] and item.get('price') == menu_item['price']:
            print("✓ Menu item data verification successful")
        else:
            print("❌ Menu item data does not match expected values")
            return False
        
        print("✓ Menu management test successful")
        
        return True
    
    except requests.exceptions.ConnectionError:
        print("Connection error: Please ensure the application is running")
        print(f"Current BASE_URL: {BASE_URL}")
        return False
    except Exception as e:
        print(f"Test error: {e}")
        return False

# Main program
if __name__ == "__main__":
    print("\nRestaurant Management System - Menu Management Test")
    print("=" * 50)
    
    result = test_menu_management()
    
    # Print result summary
    print("\n" + "=" * 50)
    print(" Test Result Summary ".center(50, '='))
    print("=" * 50)
    
    print_result("Menu Management Test (Junting Ren)", result)
    
    print("\nFinal Result: ", "✅ TEST PASSED" if result else "❌ TEST FAILED")
    print("=" * 50)