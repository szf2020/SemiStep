# Документация на протокол передачи

## Определения

**Регистр** — 16-бит holding register (Modbus).

**Word order (32-bit)** — порядок двух 16-бит слов при представлении 32-бит значений (uint32/int32/float32):
- `1 = high-word first` (сначала старшее слово, затем младшее)
- `0 = low-word first` (сначала младшее, затем старшее)

**Чанк** — набор подряд идущих holding registers, записываемый за один запрос FC16 (Write Multiple Registers). Максимум 123 регистра на запрос (ограничение протокола).

**Транзакция / bulk update** — логическая операция массовой записи данных (например загрузка таблицы из файла, вставка строки со сдвигом и т.д.), состоящая из нескольких чанков.

```

uint32 max value = 4 294 967 295
int32 max value = 2 147 483 647

int16 max value = 32 767
uint16 max value = 65 535

float32 max value = 3.4028235E+38

```

---

[String в документации Siemens](https://docs.tia.siemens.cloud/r/en-us/v21/data-types/character-strings/character-strings/string)

[WString в документации Siemens](https://docs.tia.siemens.cloud/r/en-us/v21/data-types/character-strings/character-strings/wstring-s7-1200-s7-1500-s7-1200-g2)

[WChar в документации Siemens](https://docs.tia.siemens.cloud/r/en-us/v21/data-types/character-strings/character/wchar-s7-1200-s7-1500-s7-1200-g2)

[Unicode char table](https://symbl.cc/en/unicode-table/#low-surrogates)

Кириллица в unicode в диапазоне 0400–04FF

## Общая информация

Требования протокола:
 - один Modbus Tcp сервер - PC
 - один Modbus Tcp клиент - PLC

Поддерживается работа с типами данных **float, int32, string** (внешнее представление).

Внутри PLC используются типы Siemens: **WORD**, **DWORD**, **DINT**, **REAL**, **WString**.

Обмен данными происходит с областью памяти в PLC, доступной для чтения/записи извне.

Протокол обеспечивает доступ к данным необходимым для начальной отрисовке таблицы и необходимых данных для работы в режиме исполнения.

Память в PLC состоит из следующих диапазонов:

## 1) Header

Header содержит общую информацию о протоколе и адреса областей. Читается приложением при инициализации.

Поля `int16/uint16` можно читать без знания word order - читаются в первую очередь.

| Id | Имя                    | Тип  | Описание |
|----|------------------------|------|----------|
| h1 | magic_number           | WORD | Константа для технических проверок (например 69) |
| h2 | word_order_32          | WORD | Порядок слов для 32-бит полей: `1 = high-word first`, `0 = low-word first` |
| h3 | protocol_version       | WORD | Версия протокола/разметки памяти. Сейчас `1` |

Далее адреса областей (32-бит, интерпретируются с учётом `word_order_32`):

| Id | Имя                    | Тип   | Описание |
|----|------------------------|-------|----------|
| h4 | managing_area_address  | DWORD | Адрес (0-based offset holding registers) первого регистра области `Managing area` |
| h5 | table_info_address     | DWORD | Адрес (0-based offset holding registers) первого регистра области `Table info` |
| h6 | int_data_address       | DWORD | Адрес (0-based offset holding registers) массива INT_ARRAY |
| h7 | float_data_address     | DWORD | Адрес (0-based offset holding registers) массива FLOAT_ARRAY |
| h8 | string_data_address    | DWORD | Адрес (0-based offset holding registers) массива STRING_ARRAY |
| h9 | dropdown_area_address  | DWORD | Адрес (0-based offset holding registers) области выпадающих списков |

h1 - абсолютный адрес, задаем в приложении, все отсчеты далее от него, через offset.

## 2) Managing area

`Managing area` — область для координации массовой записи и явного commit.

| Id  | Имя                | Тип   | Кто пишет | Описание |
|-----|--------------------|-------|-----------|----------|
| m1  | PC_status          | WORD  | PC        | 0 - idle, 1 - writing, 2 - commit_request |
| m2  | PC_transaction_id  | DWORD | PC        | ID транзакции (уникальный номер) |
| m3  | PC_checksum_int    | DWORD | PC        | Checksum INT_ARRAY (считается от [0..int_current_size-1]) |
| m4  | PC_checksum_float  | DWORD | PC        | Checksum FLOAT_ARRAY (считается от [0..float_current_size-1]) |
| m5  | PC_recipe_lines    | DWORD | PC        | Количество строк в рецепте (источник правды для PLC) |
| m6  | PLC_status         | WORD  | PLC       | 0 - idle, 1 - busy, 2 - crc_computing, 3 - success, 4 - error |
| m7  | PLC_error          | WORD  | PLC       | 0 - no_error, 1 - crc_int, 2 - crc_float, 3 - both, 4 - timeout |
| m8  | PLC_stored_id      | DWORD | PLC       | ID последней успешной транзакции |
| m9  | PLC_checksum_int   | DWORD | PLC       | Checksum INT_ARRAY (посчитано PLC от [0..int_current_size-1]) |
| m10 | PLC_checksum_float | DWORD | PLC       | Checksum FLOAT_ARRAY (посчитано PLC от [0..float_current_size-1]) |

### Коды ошибок (PLC_error):
- `0` — no_error
- `1` — checksum_mismatch_int
- `2` — checksum_mismatch_float
- `3` — checksum_mismatch_both
- `4` — timeout (нет активности от PC в течение заданного времени)

### Таймауты:
- **Writing (PC_status = 1):** 60 секунд (время на запись всех чанков)
- **Commit (PC_status = 2):** 30 секунд (время на расчет CRC на PLC)
- Таймеры запускаются/перезапускаются по фронтам `PC_status` (0→1 и 1→2)
- После завершения транзакции (success или error) таймеры останавливаются

### Повторные попытки:
При ошибке checksum или timeout: PC повторяет транзакцию с новым `PC_transaction_id`, максимум 3 попытки. После исчерпания попыток — сообщение об ошибке пользователю.

[Doc1](https://docs.tia.siemens.cloud/r/en-us/v21/extended-instructions-s7-1200-s7-1500-s7-1200-g2/diagnostics-s7-1200-s7-1500-s7-1200-g2/getchecksum-read-out-checksum-s7-1200-s7-1500-s7-1200-g2) - ожидается удовлетворительная стоимость подсчета crc, можно считать не чаще 1 раза в сек.

## 3) Table info

| Id | Имя             | Тип  | Описание |
|----|-----------------|------|----------|
| t1 | column_count    | DINT | Количество столбцов |
| .. | Table info data | ...  | Массив структур TableInfoStruct |

`TableInfoStruct` (фиксированного размера):

| Тип         | Имя             | Описание |
|-------------|-----------------|----------|
| WORD        | column_number   | Порядковый номер столбца |
| WString[32] | column_id       | Уникальный индекс столбца (макс 32 символа) |
| WString[32] | column_name     | UI Имя столбца (макс 32 символа) |
| WString[16] | column_units    | UI единицы измерения данных в столбце |
| WORD        | data_type       | 0 - int32, 1 - real, 2 - string |
| WORD        | column_type     | 0 - combobox, 1 - editable |
| REAL        | default_value   | Значение по умолчанию |
| REAL        | min_value       | Минимальное значение | 
| REAL        | max_value       | Максимальное значение |
| BOOL        | always_positive | Флаг всегда положительного значения |
| BOOL        | using_min       | Флаг использования min_value |
| BOOL        | using_max       | Флаг использования max_value |

Структура повторяется для каждого столбца. 

## 4) Table Content

### Array Of Int 

| Тип                  | Имя               | Описание |
|----------------------|-------------------|----------|
| DWORD                | int_area_capacity | Максимальный размер массива (в элементах) |
| DWORD                | int_current_size  | Текущее количество используемых элементов |
| array [0..N] of DINT | int_data          | Массив данных int32 |

**Примечание:** Checksum считается только от элементов `[0..int_current_size-1]`, а не от всего массива.

### Array Of Float

| Тип                  | Имя                 | Описание |
|----------------------|---------------------|----------|
| DWORD                | float_area_capacity | Максимальный размер массива (в элементах) |
| DWORD                | float_current_size  | Текущее количество используемых элементов |
| array [0..N] of REAL | float_data          | Массив данных float |

**Примечание:** Checksum считается только от элементов `[0..float_current_size-1]`, а не от всего массива.

### Array Of String

| Тип                         | Имя                  | Описание |
|-----------------------------|----------------------|----------|
| DWORD                       | string_area_capacity | Максимальный размер массива (в элементах) |
| DWORD                       | string_current_size  | Текущее количество используемых элементов |
| array [0..N] of WString[32] | string_data          | Массив данных string (макс 32 символа на элемент) |

## 5) Массивы выпадающих списков

### Таблица ссылок на все выпадающие списки: 

| id | Тип                   | Имя               | Описание |
|----|-----------------------|-------------------|----------|
| l1 | DWORD                 | dropdown_quantity | Количество выпадающих списков |
| l2 | array [0..N] of DWORD | dropdown_links    | Массив ссылок (адресов) на содержимое выпадающих списков |

### Содержимое выпадающего списка: 

| id   | Тип                         | Имя                 | Описание |
|------|-----------------------------|---------------------|----------|
| ln   | DWORD                       | N_dropdown_quantity | Количество элементов в N-м списке |
| ln+1 | array [0..N] of WString[32] | N_dropdown_data     | Содержимое N-го выпадающего списка (макс 32 символа на элемент) |

Структура повторяется для каждого выпадающего списка.

## Сценарии работы 

### Workflow транзакции:

**Шаг 0:** PC проверяет связь, читает `PLC_status`
- Если `PLC_status = 1` (busy) → ждет или сообщает ошибку "ПЛК занят"
- Если `PLC_status = 4` (error) → читает `PLC_error`, анализирует ошибку
- Если `PLC_status = 0` (idle) → продолжить

**Шаг 1:** PC устанавливает:
- `PC_status = 1` (writing)
- `PC_transaction_id = random()` (новый уникальный ID)
- `PC_recipe_lines = N` (количество строк в рецепте — источник правды для PLC)
- `PC_checksum_int = checksum(local_int_array[0..int_current_size-1])`
- `PC_checksum_float = checksum(local_float_array[0..float_current_size-1])`

**PLC детектирует фронт 0→1 в `PC_status`:**
- Запускает внутренний таймер (60 секунд на writing)
- Начинает мониторинг транзакции

**Шаг 2:** PC записывает данные чанками в рабочие области (INT_ARRAY, FLOAT_ARRAY)
- Только измененные диапазоны
- FC16 по 123 регистра максимум за запрос

**При обрыве связи на этапе 2:**
- PLC: таймер истекает (60 сек без активности)
- PLC устанавливает: `PLC_status = 4` (error), `PLC_error = 4` (timeout)
- Таблица помечена как "грязная", не может использоваться до успешной транзакции

**Шаг 3:** PC устанавливает `PC_status = 2` (commit_request)

**PLC видит переход `PC_status` 1→2:**
- Перезапускает таймер (30 секунд на расчет CRC)
- Устанавливает `PLC_status = 2` (crc_computing)

**Шаг 4:** PLC считает checksums:
- `PLC_checksum_int = GetChecksum(INT_ARRAY)` или ручной расчет
- `PLC_checksum_float = GetChecksum(FLOAT_ARRAY)`

**Шаг 5:** PLC сравнивает checksums:

**Если совпало:**
- `PLC_status = 3` (success)
- `PLC_stored_id = PC_transaction_id`
- `PLC_error = 0`

**Если НЕ совпало:**
- `PLC_status = 4` (error)
- `PLC_error = 1/2/3` (в зависимости от того, какой checksum не совпал)
- `PLC_stored_id` остается без изменений

**Если таймер истек на этапе 4-5:**
- `PLC_status = 4` (error)
- `PLC_error = 4` (timeout)

**Шаг 6:** PC читает `PLC_status` и `PLC_stored_id`:

**Если `PLC_status = 3` AND `PLC_stored_id == PC_transaction_id`:**
- Транзакция успешна
- `PC_status = 0` (idle)

**Если `PLC_status = 4`:**
- Анализ `PLC_error`:
  - `1/2/3`: Checksum mismatch → повтор транзакции (новый `transaction_id`), максимум 3 попытки
  - `4`: Timeout → проверка связи, повтор или ошибка пользователю
- `PC_status = 0` (idle)
- При необходимости: новая попытка с шага 0

**Шаг 7:** PLC видит `PC_status = 0` (idle):
- Останавливает таймер
- `PLC_status = 0` (idle)
- Готов к следующей транзакции