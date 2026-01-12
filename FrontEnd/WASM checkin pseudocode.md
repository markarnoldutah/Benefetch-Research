## PatientSelector-Section

```cs
<PatientSelectorForm mode="input" patient="_patient">

    if (mode = "input")     // or switch?
    {
        <FormSection ...>
            <FormFieldSet...>
                <Inputfields here>
                <Button type="button" onclick="addPatient">Add</Button>
                <Button type="submit" onclick="search">Search</Button>
    }

    if (mode = "search")
    {


    }

    if (mode = "selected")
    {

        
    }


</PatientSelectorForm>

@code
{
    public parameter Patient? patient = new();
    public parameter PatientSelectMode mode = "input";   // enum or magic string?

    private Patient _patient = new();

    ctor { _patient = Patient }; ?

    private void AddPatient()
    {
        - populate _patient with form values
        - _patientId = null
        - mode = "selected"
        StateHasChanged()?
    }

}

```
