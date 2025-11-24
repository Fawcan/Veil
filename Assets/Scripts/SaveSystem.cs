using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

public static class SaveSystem
{
    private static readonly string savePath = Application.persistentDataPath + "/game_save.veil"; // .veil is just a cool extension name
    
    // ⚠️ IMPORTANT: In a real commercial game, manage these keys more securely!
    private static readonly string encryptionKey = "1234567890123456"; // Must be 16 chars
    private static readonly string initializationVector = "1234567890123456"; // Must be 16 chars

    public static void SaveGame(SaveData data)
    {
        BinaryFormatter formatter = new BinaryFormatter();

        using (FileStream fs = new FileStream(savePath, FileMode.Create))
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(encryptionKey);
                aes.IV = Encoding.UTF8.GetBytes(initializationVector);

                using (CryptoStream cs = new CryptoStream(fs, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    formatter.Serialize(cs, data);
                }
            }
        }
        
        Debug.Log("Game Saved to: " + savePath);
    }

    public static SaveData LoadGame()
    {
        if (File.Exists(savePath))
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();

                using (FileStream fs = new FileStream(savePath, FileMode.Open))
                {
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = Encoding.UTF8.GetBytes(encryptionKey);
                        aes.IV = Encoding.UTF8.GetBytes(initializationVector);

                        using (CryptoStream cs = new CryptoStream(fs, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            SaveData data = formatter.Deserialize(cs) as SaveData;
                            return data;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to load save file (Decryption Error?): " + e.Message);
                return null;
            }
        }
        else
        {
            Debug.LogWarning("Save file not found in " + savePath);
            return null;
        }
    }
}