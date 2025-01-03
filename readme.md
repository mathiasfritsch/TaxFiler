import in excel with


= Table.TransformColumnTypes(
    #"Höher gestufte Header",
    {
        {"Buchungsdatum", Date.Type},
        {"Auftraggeber/Empfängeger", type text},
        {"Beschreibung", type text},
        {"Zahlungsbetrag", type number},
        {"Begleitende Dokumente", type text},
        {"Mwst Satz", type number},
        {"UST", type number},
        {"Transaktionsreferenz", type text},
        {"Ausgehend", type logical},
        {"Ust relevant", type logical},
        {"Einkommenssteuer relevant", type logical}
    },
    "en-US"
)