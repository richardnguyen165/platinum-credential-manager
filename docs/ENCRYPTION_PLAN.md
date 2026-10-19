# Plan: Encrypting Credential Data

Goal: encrypt every stored field with reversible, authenticated encryption (AES-GCM), using only the built-in .NET crypto classes. Do one step at a time and ignore the later ones until you get there.

Ground rules:
- All data is encrypted, so database-side search, sort and duplicate checks on those fields stop working on ciphertext.
- Never write your own algorithm or scheme.
- Keys never live in the database or the repo.

## Step 1: Learn the core idea (about 1 hour)

Read an introduction to symmetric encryption and AES, then explain these in your own words.

- [ ] Key, plaintext and ciphertext
- [ ] Nonce (IV): why it is random and used once per encryption
- [ ] Authenticated encryption (AES-GCM): what the tag protects against
- [ ] Self-check: why does encrypting the same password twice give different output?

## Step 2: Play in a scratch project (about 1-2 hours)

Use a throwaway console project, not the real app.

- [ ] Encrypt a string and decrypt it again, using the .NET AES-GCM docs
- [ ] Flip one byte of the ciphertext and try to decrypt. Notice what fails
- [ ] Decrypt with the wrong key. Notice what fails
- [ ] Don't worry about key storage yet. Use a hardcoded test key

## Step 3: Decide your design on paper (about 30 minutes)

Write short answers to these four questions.

1. Which fields are encrypted? (Decided: all of them.)
2. Which app features compare, search or sort on those fields? Check duplicate detection, search, and CSV import/export.
3. Where does the key live in development and in production?
4. What happens to the rows already in the database?

## Step 4: Learn the two integration pieces (about 2 hours)

- [ ] Key storage: user secrets for development, environment variables or a secrets manager for production
- [ ] EF Core value converters: how they transform a value on save and on load

## Step 5: Build it (the real work)

- [ ] Start with one field on one entity, like a password
- [ ] Get save, load and the existing features working
- [ ] Extend to the other fields
- [ ] Extend to CSV import and export
- [ ] Plan the migration of existing data last

## Step 6: Test it

- [ ] Look at the raw database rows and confirm you see only ciphertext
- [ ] Re-run the CSV test files in `PlatinumCredentialManager.Client/public/test-csv/`, since every import and export path touches encrypted fields
- [ ] Check logs and error messages never contain decrypted secrets

## Park for later

You don't need these for a first working version.

- Key rotation and key versioning
- Envelope encryption
- Transparent Data Encryption (TDE) at the database level
- Per-user keys
- Deterministic encryption and blind indexes (for searching ciphertext)

## Maybe: hashing for frontend-to-backend security (to evaluate)

Idea: add hashing so data sent from the frontend to the backend is secure. Research this before building it, because hashing may not be the right tool for this goal.

- Hashing is one-way. The backend cannot read a hashed password or username back, so it can't store it for the user to see later. It only works for values that are verified, never retrieved.
- Securing data in transit is the job of TLS (HTTPS) between the browser and the API, and an encrypted database connection. Check that both are in place first.
- Hashing in the browser doesn't protect a login: an attacker who captures the hash can just send the hash. Look up "pass-the-hash" and why the hash effectively becomes the password.
- Login passwords are handled by Keycloak, so check whether any hashing is needed in this app at all.

Questions to answer:
- [ ] Is HTTPS enforced everywhere (browser to API, API to Keycloak, API to database)?
- [ ] Which values actually need verifying rather than reading back? If none, hashing has no place.
- [ ] If the real concern is a network attacker, is TLS plus a correct CORS and token setup enough?
- [ ] Decide: keep, or drop this idea after the research.

## Terms

- **Nonce (IV):** random value used once per encryption, stored next to the ciphertext. Not secret.
- **AEAD / AES-GCM:** encryption that also produces a tag, so tampering is detected on decrypt.
- **HMAC:** a keyed hash. Same input gives the same output, so it can be used to find duplicates without decrypting.
- **Envelope encryption:** data is encrypted with a data key, and that key is encrypted with a master key.
- **TDE:** the database encrypts its files on disk. It does not hide data from anyone who can query the database.
