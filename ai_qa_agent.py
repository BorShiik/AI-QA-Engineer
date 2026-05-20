import requests
import json
import os
import re
import sys
import time
from dotenv import load_dotenv

# Reconfigure stdout to use utf-8 to prevent UnicodeEncodeErrors on Windows terminals with emojis
try:
    sys.stdout.reconfigure(encoding='utf-8')
except AttributeError:
    pass

load_dotenv()

API_KEY = os.getenv("GEMINI_API_KEY")

if not API_KEY:
    print("❌ Błąd: Nie znaleziono klucza API! Upewnij się, że plik .env istnieje i zawiera zmienną GEMINI_API_KEY.")
    sys.exit(1)

MODEL_NAME = os.getenv("GEMINI_MODEL", "gemini-3.5-flash")
URL = f"https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={API_KEY}"


def uruchom_ai_qa_agent(sciezka_pliku):
    if not os.path.exists(sciezka_pliku):
        print(f"❌ Błąd: Plik źródłowy '{sciezka_pliku}' nie został znaleziony!")
        return
    try:
        with open(sciezka_pliku, 'r', encoding='utf-8') as f:
            kod_z_pliku = f.read()
    except Exception as e:
        print(f"❌ Nie udało się odczytać pliku: {e}")
        return

    print(f"📄 Plik '{sciezka_pliku}' wczytany poprawnie. Uruchamianie procedury AI QA Engineer...")

    backticks = chr(96) * 3

    prompt = f"""
Jesteś doświadczonym inżynierem oprogramowania (Senior QA Engineer / Developer). Twoim zadaniem jest przeprowadzenie rygorystycznego Code Review oraz napisanie testów jednostkowych dla dostarczonego kodu.

Działaj według poniższych kroków i sformatuj swoją odpowiedź dokładnie w dwóch sekcjach:

**SEKCJA 1: CODE REVIEW**
Zanalizuj kod pod kątem:
1. Złożoności obliczeniowej i potencjalnych problemów z wydajnością.
2. Błędów logicznych i ukrytych bugów (edge cases).
3. Dobrych praktyk i czystości kodu (np. nazewnictwo zmiennych, formatowanie).
Wypunktuj krótko i zwięźle, co należy poprawić i dlaczego.

**SEKCJA 2: TESTY JEDNOSTKOWE**
Wygeneruj kompletny kod testów jednostkowych dla dostarczonego skryptu.
- Użyj standardowych bibliotek testowych dla języka, w którym napisano kod (np. pytest dla Pythona, xUnit dla C#, Jest dla TypeScript/JavaScript).
- Pokryj zarówno ścieżki pozytywne (happy path), jak i przypadki brzegowe (np. puste dane, błędne typy).
- Zwróć WYŁĄCZNIE działający kod testów, bez żadnych dodatkowych komentarzy czy wyjaśnień poza kodem.
- Cały kod testów musi być absolutnie i bezwzględnie otoczony blokiem markdown z określeniem języka, na przykład:
{backticks}python
# tutaj kod testów
{backticks}

**KOD DO ANALIZY:**
{kod_z_pliku}
"""

    payload = {
        "contents": [{
            "parts": [{"text": prompt}]
        }],
        "generationConfig": {
            "temperature": 0.0
        }
    }

    naglowki = {'Content-Type': 'application/json'}

    print("🚀 Wysyłanie zapytania do Google AI Studio za pomocą metody POST...")
    
    max_retries = 3
    retry_delay = 5
    backoff_factor = 2
    odpowiedz = None

    for attempt in range(max_retries + 1):
        try:
            odpowiedz = requests.post(URL, headers=naglowki, json=payload)
            
            if odpowiedz.status_code == 404:
                print(f"❌ Błąd 404: Nieprawidłowa nazwa modelu '{MODEL_NAME}' lub wadliwy adres URL endpointu.")
                return
            elif odpowiedz.status_code == 429:
                if attempt < max_retries:
                    wait_time = retry_delay * (backoff_factor ** attempt)
                    try:
                        err_json = odpowiedz.json()
                        for detail in err_json.get('error', {}).get('details', []):
                            if detail.get('@type') == 'type.googleapis.com/google.rpc.RetryInfo':
                                delay_str = detail.get('retryDelay', '')
                                match = re.match(r'([\d.]+)\s*s', delay_str)
                                if match:
                                    wait_time = float(match.group(1)) + 1
                                    break
                    except Exception:
                        pass
                    print(f"⚠️ Otrzymano kod 429 (Rate Limit) dla modelu '{MODEL_NAME}'. Ponowna próba {attempt + 1}/{max_retries} za {wait_time:.1f}s...")
                    time.sleep(wait_time)
                    continue
                else:
                    print(f"❌ Błąd 429: Przekroczono limit zapytań (Rate Limit) dla modelu '{MODEL_NAME}'. Odczekaj minutę.")
                    return
            elif odpowiedz.status_code == 503:
                if attempt < max_retries:
                    wait_time = retry_delay * (backoff_factor ** attempt)
                    print(f"⚠️ Otrzymano kod 503 (Serwer przeciążony). Ponowna próba {attempt + 1}/{max_retries} za {wait_time:.1f}s...")
                    time.sleep(wait_time)
                    continue
                else:
                    print("❌ Błąd 503: Serwery Google są przeciążone. Spróbuj ponownie później.")
                    return
            elif odpowiedz.status_code != 200:
                print(f"❌ Błąd serwera ({odpowiedz.status_code}): {odpowiedz.text}")
                return
            
            break
        except requests.exceptions.RequestException as e:
            if attempt < max_retries:
                wait_time = retry_delay * (backoff_factor ** attempt)
                print(f"⚠️ Błąd sieci: {e}. Ponowna próba {attempt + 1}/{max_retries} za {wait_time:.1f}s...")
                time.sleep(wait_time)
                continue
            else:
                print(f"❌ Błąd połączenia sieciowego: {e}")
                return

    try:
        dane_json = odpowiedz.json()
        pelny_wynik = dane_json['candidates'][0]['content']['parts'][0]['text']

        print("\n" + "="*60)
        print("🤖 ANALIZA PORÓWNAWCZA I INSPEKCJA AI (CODE REVIEW):")
        print("="*60)
        
        regex_pattern = rf'{backticks}(?:[a-zA-Z0-9]+)?\n.*?{backticks}'
        tekst_review = re.sub(regex_pattern, '\n[KOD TESTÓW JEDNOSTKOWYCH ZOSTAŁ WYCIĘTY I ZAPISANY DO PLIKU]', pelny_wynik, flags=re.DOTALL)
        print(tekst_review.strip())
        regex_extract = rf'{backticks}(?:[a-zA-Z0-9]+)?\n(.*?){backticks}'
        dopasowanie_kodu = re.search(regex_extract, pelny_wynik, flags=re.DOTALL)
        
        if dopasowanie_kodu:
            wyekstrahowany_kod = dopasowanie_kodu.group(1).strip()
            
            katalog = os.path.dirname(sciezka_pliku)
            nazwa_bazowa = os.path.basename(sciezka_pliku)
            nazwa_pliku_testu = f"test_{nazwa_bazowa}"
            sciezka_pliku_testu = os.path.join(katalog, nazwa_pliku_testu) if katalog else nazwa_pliku_testu
            
            with open(sciezka_pliku_testu, 'w', encoding='utf-8') as f_out:
                f_out.write(wyekstrahowany_kod)
                
            print("\n" + "="*60)
            print(f"✅ SUKCES: Kod testów został automatycznie wygenerowany i zapisany!")
            print(f"📍 Lokalizacja pliku: {sciezka_pliku_testu}")
            print("="*60)
        else:
            print("\n⚠️ Uwaga: Agent nie zwrócił poprawnego bloku kodu markdown. Nie udało się zapisać pliku testów.")

    except Exception as e:
        print(f"❌ Wystąpił niespodziewany błąd podczas działania programu: {e}")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        target_file = sys.argv[1]
    else:
        target_file = "target_file.py" 
        
    uruchom_ai_qa_agent(target_file)