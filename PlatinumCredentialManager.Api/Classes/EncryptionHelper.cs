using System.Security.Cryptography;
using System.Text;

public class EncryptionHelper()
{
    private const byte Version = 1;

    public byte[] GetKey()
    {
        var value = Environment.GetEnvironmentVariable("KEY");
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException("Environment variable 'KEY' must be set");
        }
        var key = Convert.FromBase64String(value);
        if (key.Length != 32)
        {
            throw new InvalidOperationException("Key must be 32 characters long!");
        }
        return key;
    }

    public string Encrypt(string data)
    {
        var key = GetKey();

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // MaxSize = 12 (this also the IV)
        // 12 bytes, 96 bit, the chance of two random nonces is very low when using the same key
        RandomNumberGenerator.Fill(nonce);

        // After ~2^32 messages per key, the chance of a random nonce repeating stops being negligible, so rotate the key before then

        var plainTextBytes = Encoding.UTF8.GetBytes(data);
        var cipherText = new byte[plainTextBytes.Length]; // platextbytes always the same size as cipher text bytes
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // MaxSize = 16, authenticatipon tag

        aes.Encrypt(nonce, plainTextBytes, cipherText, tag, new byte[] { Version });

        byte[] packed = [Version, .. nonce, .. cipherText, .. tag]; // packed string consisting of Version, nonce, cipherText and tag
        // Version, small label, one byte, states tghat the blog was made using format/scheme number X
        //scystems change over time, tells receiver what algorithm to decrypt.
        // PUBLIC also protected by tag so that attacker cannot change the version number to trick the system into using a weaker metod

        // nonce: fresh, 12 bytes, unique value with the message, which you already know well. 
        // combined with the key to generate the keystream
        // public needed for decryption, and must never repeat with the same key

        // keystream: random-looking bytes,generated from the key + nonce, gets mixed into your message and mix the two together
        // key + nonce, mixed into message to hide ti

        // derived from one-time pad => problem is that to encrypt 1GD file, you need a 1GD secret shared in advacne, and can never reuse it
        // a keystream sollves it by sharing a short key (32 butes), and an algorithm stretches it into as mch random-looking data as much as you need it
        // looks completely random, deterministic (thats wghy reusuing a nonce is dangerous)

        // ciphertext: the encrypted message, plaintext mixed with the keystream, same length as original message
        // unreadable without the key, hides the content, but not the length

        // tag: tamper-eveident seal, usually 16 bytes
        // calculated from key, ciphertext, and version, and proves the message was not changed in anay way, and created by someone who has the key

        return Convert.ToBase64String(packed);
    }

    public string Decrypt(string storedData)
    {
        var key = GetKey();

        var data = Convert.FromBase64String(storedData);
        if (data.Length < 29) throw new CryptographicException("Stored data is too short to be valid"); // 1 version + 12 nonce + 16 tag
        if (data[0] != Version) throw new CryptographicException("Unknown version");
        var nonce = data.AsSpan(1, 12); // nonce is 12 bytes long
        var tag = data.AsSpan(data.Length - 16); // tag is 16 bytes long, start fbefore the last 16th byte
        var cipherText = data.AsSpan(13, data.Length - 13 - 16); // start from the 13th byte, after the nonce, start from the byte - tag - version
        var plain = new byte[cipherText.Length]; // start in plain text
        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(nonce, cipherText, tag, plain, [Version]); // throws if tampered
        return Encoding.UTF8.GetString(plain);  
    }
}