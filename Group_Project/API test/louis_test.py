"""
Restaurant Management System API Test - Authentication
Developer: Louis He

This script tests user registration and login functionality.

Usage:
    python louis_auth_test.py
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

def test_authentication():
    print_header("Authentication Test (Louis He)")
    
    try:
        # 1. Register a new user
        timestamp = int(time.time())
        email = f"test_user_{timestamp}@example.com"
        
        print("1. Registering a new user...")
        session = requests.Session()
        register_data = {
            "firstName": "Test",
            "lastName": "User",
            "email": email,
            "password": "Test123!",
            "confirmPassword": "Test123!",
            "phoneNumber": "1234567890"
        }
        
        response = session.post(
            f"{BASE_URL}/api/Auth/register", 
            json=register_data,
            verify=False
        )
        
        if response.status_code != 200:
            print(f"Registration failed: HTTP {response.status_code}")
            print(f"Response: {response.text}")
            return False
        
        print("✓ User registration successful")
        
        # 2. Login with the new user
        print("\n2. Logging in with the new user...")
        login_data = {
            "email": email,
            "password": "Test123!",
            "rememberMe": False
        }
        
        response = session.post(
            f"{BASE_URL}/api/Auth/login", 
            json=login_data,
            verify=False
        )
        
        if response.status_code != 200:
            print(f"Login failed: HTTP {response.status_code}")
            print(f"Response: {response.text}")
            return False
        
        # Check if authentication cookie exists
        cookies = session.cookies.get_dict()
        if not any('.AspNetCore' in key for key in cookies):
            print("❌ Failed to receive authentication cookie")
            return False
        
        print("✓ User login successful")
        print("✓ Authentication cookie received")
        
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
    print("\nRestaurant Management System - Authentication Test")
    print("=" * 50)
    
    result = test_authentication()
    
    # Print result summary
    print("\n" + "=" * 50)
    print(" Test Result Summary ".center(50, '='))
    print("=" * 50)
    
    print_result("Authentication Test (Louis He)", result)
    
    print("\nFinal Result: ", "✅ TEST PASSED" if result else "❌ TEST FAILED")
    print("=" * 50)