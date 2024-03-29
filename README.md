# revit-plugin-jumpers
## Описание
Данный репозиторий содержит плагин для Autodesk Revit 2021 на C#. Этот плагин реализует автоматическое размещение в проекте заранее созданных семейств перемычек над оконными и дверными проемами. Плагин позволяет размещать по условиям выбора перемычки над оконными или дверными проемами с учетом типов стен, в которых они размещаются.
## Окно ввода данных
![Рисунок1](https://github.com/Nikashi00/revit-plugin-jumpers/assets/147995583/b24f630b-a214-4062-9317-15e99a18aedc)
## Функционал
- [x] Размещение по условиям выбора (либо у выбранных проемов, либо у видимых на виде, либо у всех в модели)
- [x] Изменение при корректировке родительского семейства (смещение, корректировка длины, удаление)
- [ ] Создание дополнительных типов используемых семейств (при необходимости)
- [ ] Формирование сечений в легенде
- [x] Адаптивность под разные применяемые семейства (возможность настройки анализируемых параметров в зависимости от применяемых семейств и сохранения настроек).
- [x] Визуализация результата работы (возможность выбора/перехода на вид через интерфейс)
## Видео демонстрация работы
https://youtu.be/sqwRiNz3Q2k
## Требования к размещаемому семейству
* Элемент категории "Каркас несущий"
* Наличие параметров перемычки: Ширина проема, Длина опирания 1, Длина опирания 2
