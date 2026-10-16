"""Canonical signing primitives, with domain separation and Ed25519."""
import base64
import hashlib
import json
import os
from pathlib import Path

from cryptography.exceptions import InvalidSignature
from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey, Ed25519PublicKey


def canonical(obj):
    return json.dumps(obj, sort_keys=True, separators=(",", ":"), ensure_ascii=True).encode("utf-8")


def sha(obj):
    return hashlib.sha256(canonical(obj)).hexdigest()


def save_key(directory: Path, name: str):
    directory.mkdir(parents=True, exist_ok=True, mode=0o700)
    key = Ed25519PrivateKey.generate()
    path = directory / f"{name}.pem"
    with os.fdopen(os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600), "wb") as f:
        f.write(key.private_bytes(serialization.Encoding.PEM, serialization.PrivateFormat.PKCS8, serialization.NoEncryption()))
    return public_key(key)


def load_private(path: Path):
    return serialization.load_pem_private_key(path.read_bytes(), password=None)


def public_key(key):
    return base64.b64encode(key.public_key().public_bytes(serialization.Encoding.Raw, serialization.PublicFormat.Raw)).decode("ascii")


def sign(key, domain: str, payload):
    return base64.b64encode(key.sign(domain.encode() + b"\0" + canonical(payload))).decode("ascii")


def verify(public_key_b64, domain: str, payload, signature_b64):
    try:
        pub = Ed25519PublicKey.from_public_bytes(base64.b64decode(public_key_b64, validate=True))
        pub.verify(base64.b64decode(signature_b64, validate=True), domain.encode() + b"\0" + canonical(payload))
        return True
    except (ValueError, InvalidSignature, TypeError):
        return False
