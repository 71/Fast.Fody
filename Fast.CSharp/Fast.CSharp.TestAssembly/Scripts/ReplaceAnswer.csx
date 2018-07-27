#load "../../Fast.CSharp.Fody/Context.csx"

Context.Warn("Starting replacement...");

Context.Properties
    .Where(x => x.Name == "answer")
    .Subscribe(prop => {
        Context.Debug("Found property to edit in {0}, processing...", prop.DeclaringType.FullName);

        MethodBody newBody = new MethodBody(prop.GetMethod);

        newBody.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 42));
        newBody.Instructions.Add(Instruction.Create(OpCodes.Ret));

        prop.GetMethod.Body = newBody;
    });
