"C:\Program Files\Java\jdk1.8.0_161\bin\keytool" -exportcert -alias "x2block_2022" -keystore x2block_2022.keystore | openssl sha1 -binary| openssl base64

@pause