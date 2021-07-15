# Example client library for omnikeeper in Python

## Setting up and running example 
```bash
pip install -r requirements.txt
python main.py
```
Should return a list of CIIDs from the acme-dev omnikeeper instance.

## Using client library in other projects

### get generated python client library
```bash
python -m pip install git+https://www.github.com/maximiliancsuk/omnikeeper-client-python.git
```

### Usage of client library
see main.py

## Authentication
Authentication is done via username + password. Make sure chosen user exists in keycloak. Make sure oauth settings are configured appropriately.