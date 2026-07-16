# Обзор развёртывания COBOL-приложений

## Что удалось проверить

### Простой Core Banking System

[Простой Core Banking System](https://github.com/fzn0x/core-banking-system)
был полностью развёрнут на Ubuntu:

- установлен компилятор GnuCOBOL;
- COBOL-код скомпилирован в обычную Linux-программу;
- через терминал проверены пополнение, снятие и отчёт по счетам.

GnuCOBOL — это компилятор, а не интерпретатор. Упрощённо процесс выглядит так:

```text
COBOL-код -> GnuCOBOL -> C-компилятор -> Linux-программа
```

Это сработало, потому что приложение использует стандартный COBOL и локальные
файлы, без зависимости от mainframe-инфраструктуры.

### IBM Bank of Z

Для [IBM Bank of Z](https://github.com/IBM/Bank-of-Z) на Ubuntu удалось
развернуть компоненты, которые поддерживают обычную Linux-среду:

- web UI;
- Node.js proxy;
- IBM z/OS Connect Designer;
- собранный API-модуль `api.war`.

COBOL/PL/I банковское ядро не запущено, поскольку оно требует полноценную IBM
dev/test-среду: z/OS, CICS, IMS и Db2 for z/OS. Проект также использует JCL,
BMS maps, RACF и IBM runtime API.

## Почему недостаточно обычного Docker

Обычный Docker-контейнер использует ядро операционной системы хоста. Поэтому
контейнер на Ubuntu остаётся Linux-средой и не предоставляет z/OS, CICS, IMS
или Db2 for z/OS.

IBM ZD&T действительно умеет использовать Docker для развёртывания, но это не
обычный публичный image приложения. Он создаёт лицензированную эмулированную
z/OS-среду с IBM middleware.

## Что необходимо компании для полной проверки

Нужен один из следующих вариантов:

1. [IBM Wazi as a Service](https://www.ibm.com/products/wazi-as-a-service) —
   dev/test-среда z/OS в IBM Cloud. Доступен переход **Try it now**, стоимость
   предоставляется через представителя IBM.
2. [IBM Z Development and Test Environment](https://www.ibm.com/products/z-development-test-environment) —
   эмулированная z/OS-среда для x86-инфраструктуры. IBM предлагает варианты
   **Try it free** и **Purchase now**; поставляемая среда может включать CICS,
   IMS, Db2, COBOL, PL/I и z/OS Connect.
3. [IBM Z Software Trials](https://www.ibm.com/products/z/trials) — бесплатные
   виртуальные z/OS trials с ограничением по времени. Необходимо отдельно
   подтвердить у IBM возможность загрузки собственного Bank of Z, поскольку
   часть trials ограничена готовыми учебными сценариями.
4. Доступ к существующей [IBM Z](https://www.ibm.com/products/z) dev/test-среде
   компании, клиента или IBM-партнёра.

## Рекомендация

Запросить у IBM или партнёра **Wazi as a Service PoC** либо **ZD&T evaluation**
с CICS, IMS, Db2, COBOL, PL/I и z/OS Connect, а также с правом загрузки
собственного кода Bank of Z.

На таком стенде можно развернуть реальные legacy-программы, выполнить CICS/IMS
транзакции и batch jobs и проверить карту паритета на работающей системе. До
получения стенда разработку можно продолжать по коду и мокам, но mainframe-
сценарии должны оставаться явно непроверенными.
