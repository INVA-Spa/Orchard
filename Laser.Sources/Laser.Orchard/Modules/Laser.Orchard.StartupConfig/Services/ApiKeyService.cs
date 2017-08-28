﻿using Orchard;
using Orchard.Environment.Configuration;
using Orchard.Logging;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using Laser.Orchard.StartupConfig.WebApiProtection.Models;
using Orchard.ContentManagement;
using Orchard.Utility.Extensions;
using System.Text;
using Orchard.Environment.Extensions;
using Orchard.Caching.Services;

namespace Laser.Orchard.StartupConfig.Services {
    [OrchardFeature("Laser.Orchard.StartupConfig.WebApiProtection")]
    public class ApiKeyService : IApiKeyService {
        private readonly IOrchardServices _orchardServices;
        private readonly ShellSettings _shellSettings;
        private HttpRequest _request;
        public ILogger Logger;
        private ICacheStorageProvider _cacheStorage;

        public ApiKeyService(ShellSettings shellSettings, IOrchardServices orchardServices, ICacheStorageProvider cacheManager) {
            _shellSettings = shellSettings;
            _orchardServices = orchardServices;
            Logger = NullLogger.Instance;
            _cacheStorage = cacheManager;
        }
        public string ValidateRequestByApiKey(string additionalCacheKey, bool protectAlways = false) {
            _request = HttpContext.Current.Request;
            if (additionalCacheKey != null) {
                return additionalCacheKey;
            }

            bool check = false;
            if (protectAlways == false) {
                var settings = _orchardServices.WorkContext.CurrentSite.As<ProtectionSettingsPart>();
                if (String.IsNullOrWhiteSpace(settings.ProtectedEntries)) {
                    return additionalCacheKey; // non ci sono entries da proteggere
                }
                var protectedControllers = settings.ProtectedEntries.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var area = _request.RequestContext.RouteData.Values["area"];
                var controller = _request.RequestContext.RouteData.Values["controller"];
                var action = _request.RequestContext.RouteData.Values["action"];
                var entryToVerify = "";
                if (action == null) {
                    // caso che si verifica con le web api (ApiController)
                    entryToVerify = String.Format("{0}.{1}", area, controller);
                } else {
                    // caso che si verifica con i normali Controller
                    entryToVerify = String.Format("{0}.{1}.{2}", area, controller, action);
                }
                if (protectedControllers.Contains(entryToVerify, StringComparer.InvariantCultureIgnoreCase)) {
                    check = true;
                }
            } else {
                check = true;
            }

            if (check == true) {
                var myApikey = _request.QueryString["ApiKey"] ?? _request.Headers["ApiKey"];
                var myAkiv = _request.QueryString["AKIV"] ?? _request.Headers["AKIV"];
                if (!TryValidateKey(myApikey, myAkiv, (_request.QueryString["ApiKey"] != null && _request.QueryString["clear"] != "false"))) {
                    additionalCacheKey = "UnauthorizedApi";
                } else {
                    additionalCacheKey = "AuthorizedApi";
                }
            }
            return additionalCacheKey;
        }

        public string GetValidApiKey(string sIV, bool useTimeStamp = false) {
            string key = "";
            byte[] mykey = _shellSettings.EncryptionKey.ToByteArray();
            byte[] myiv = Convert.FromBase64String(sIV);
            try {
                var settings = _orchardServices.WorkContext.CurrentSite.As<ProtectionSettingsPart>();
                var defaulApp = settings.ExternalApplicationList.ExternalApplications.First();
                string aux = defaulApp.ApiKey;
                if (useTimeStamp) {
                    Random rnd = new Random();
                    aux += ":" + ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString() + ":" + rnd.Next(1000000).ToString();
                }

                byte[] encryptedAES = EncryptStringToBytes_Aes(aux, mykey, myiv);
                key = Convert.ToBase64String(encryptedAES);
            } catch {
                // ignora volutamente qualsiasi errore e restituisce una stringa vuota
            }
            return key;
        }

        private bool TryValidateKey(string token, string akiv, bool clearText) {
            byte[] mykey = _shellSettings.EncryptionKey.ToByteArray();
            string cacheKey;
            _request = HttpContext.Current.Request;
            try {
                byte[] myiv = Convert.FromBase64String(akiv);
                if (String.IsNullOrWhiteSpace(token)) {
                    Logger.Error("Empty Token");
                    return false;
                }

                string key = token;
                if (!clearText) {
                    var encryptedAES = Convert.FromBase64String(token);
                    key = DecryptStringFromBytes_Aes(encryptedAES, mykey, myiv);
                    //key = aes.Decrypt(token, mykey, myiv);
                } else {
                    var encryptedAES = EncryptStringToBytes_Aes(token, mykey, myiv);
                    var base64EncryptedAES = Convert.ToBase64String(encryptedAES, Base64FormattingOptions.None);
                    //var encrypted = aes.Crypt(token, mykey, myiv);
                    if (_request.QueryString["unmask"] == "true") {
                        HttpContext.Current.Response.Clear();
                        HttpContext.Current.Response.Write("Encoded: " + HttpContext.Current.Server.UrlEncode(base64EncryptedAES) + "<br/>");
                        HttpContext.Current.Response.Write("Clear: " + base64EncryptedAES);
                        HttpContext.Current.Response.End();
                    }
                }
                // test if key has a time stamp
                var tokens = key.Split(':');
                string pureKey = tokens[0];
                int unixTimeStamp, unixTimeStampNow;
                unixTimeStamp = 0;
                unixTimeStampNow = ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
                if (tokens.Length >= 2) {
                    unixTimeStamp = Convert.ToInt32(tokens[1]);
                }
                var settings = _orchardServices.WorkContext.CurrentSite.As<ProtectionSettingsPart>();
                var item = settings.ExternalApplicationList.ExternalApplications.FirstOrDefault(x => x.ApiKey.Equals(pureKey));
                if (item == null) {
                    Logger.Error("Decrypted key not found: key = " + key);
                    return false;
                }


                var floorLimit = Math.Abs(unixTimeStampNow - unixTimeStamp);
                if (item.EnableTimeStampVerification) {
                    cacheKey = String.Concat(_shellSettings.Name, token);
                    if (_cacheStorage.Get<object>(cacheKey) != null) {
                        Logger.Error("cachekey duplicated: key = " + key);
                        return false;
                    }
                    if (floorLimit > ((item.Validity > 0 ? item.Validity : 5)/*minutes*/ * 60)) {
                        Logger.Error("Timestamp validity expired: key = " + key);
                        return false;
                    } else {
                        _cacheStorage.Put(cacheKey, "", new TimeSpan(0, item.Validity > 0 ? item.Validity : 5, 0));
                    }
                }
                return true;
            } catch (Exception ex) {
                Logger.Error("Exception: " + ex.Message);
                return false;
            }
        }


        private byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV) {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an AesCryptoServiceProvider object
            // with the specified key and IV.
            using (AesCryptoServiceProvider aesAlg = CreateCryptoService(Key, IV)) {

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream()) {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        private string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV) {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an AesCryptoServiceProvider object
            // with the specified key and IV.
            using (AesCryptoServiceProvider aesAlg = CreateCryptoService(Key, IV)) {
                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt)) {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        private AesCryptoServiceProvider CreateCryptoService(byte[] key, byte[] iv) {
            AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider();
            aesAlg.Key = key;
            aesAlg.IV = iv;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;
            return aesAlg;
        }
    }
}