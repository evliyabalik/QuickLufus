# QuickLufus

Linux için hafif USB ISO yazma aracı.

## Kurulum

```bash
# Single-file executable
wget https://github.com/evliyabalik/QuickLufus/releases/download/v1.0/Lufus
chmod +x Lufus
./Lufus

# Veya kaynak koddan
git clone https://github.com/evliyabalik/QuickLufus.git
cd QuickLufus/Lufus
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o ./publish
./publish/Lufus
```
##Kullanım
ISO dosyası seç
USB cihaz seç (sda otomatik hariç)
Yazdır butonuna tıkla

##Gereksinimler
Linux
pkexec

##Uyarı
USB'deki tüm veriler silinir.

##Lisans
MIT
