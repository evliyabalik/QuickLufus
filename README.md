# QuickLufus

Linux için hafif USB ISO yazma aracı.

## Kurulum

```bash
wget https://github.com/evliyabalik/QuickLufus/releases/download/v1.0/Lufus
chmod +x Lufus
./Lufus
```

Kaynak koddan:

```bash
git clone https://github.com/evliyabalik/QuickLufus.git
cd QuickLufus/Lufus
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o ./publish
./publish/Lufus
```

## Kullanım

1. ISO dosyası seç
2. USB cihaz seç (sda otomatik hariç)
3. Yazdır butonuna tıkla

## Gereksinimler

- Linux
- pkexec

## Uyarı

USB'deki tüm veriler silinir.

## Lisans

MIT
