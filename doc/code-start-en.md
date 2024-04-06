# Getting Started

## Structure

The entry point of the program is the IPlugin interface provided by the official, triggered through the query function. Below is an explanation of the main functions of each class.

### Translater

Various basic functionalities of the plugin, properties of the plugin, implementation of various basic interfaces provided by the official (such as setting, querying).

### Utils

Utility space, containing common functionalities.

### TranslateHelper

Adapter for translation functionalities, providing directly callable translation interface functions to the upper layer (Translater), responsible for scheduling the implementation of various translation APIs downwards, ensuring stable and fast translation.

### SuggestHelper

Adapter for suggestion functionalities, currently serving as the implementation of the suggest interface.

### HistoryHelper

Adapter for history functionalities.

### ITranslater

Specific translation implementation (usually network interface calls).

## How to Add a Translation Implementation

Let's say we want to add Google translation.

1. Create a folder named Google.
2. Create GoogleTranslator.cs.
3. Create a class GoogleTranslator inheriting from ITranslater and implement the interface.
4. Add GoogleTranslator to the translatorTypes in the constructor of TranslateHelper.
5. Test if the call is successful.

That's how you add a new translation interface!
