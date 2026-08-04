const toggle=document.querySelector('.menu-toggle');const nav=document.querySelector('.main-nav');toggle?.addEventListener('click',()=>{const open=nav?.classList.toggle('open')??false;toggle.setAttribute('aria-expanded',String(open));});
const price=document.querySelector('[data-base-price]');document.querySelectorAll('.product-option').forEach(x=>x.addEventListener('change',()=>{const base=Number(price?.dataset.basePrice??0);const extras=[...document.querySelectorAll('.product-option option:checked')].reduce((sum,o)=>sum+Number(o.dataset.price??0),0);if(price)price.textContent=new Intl.NumberFormat('cs-CZ',{style:'currency',currency:'CZK',maximumFractionDigits:0}).format(base+extras);}));

document.querySelectorAll('[data-product-gallery]').forEach(gallery=>{
    const main=gallery.querySelector('[data-product-gallery-main]');
    const thumbs=[...gallery.querySelectorAll('[data-product-thumb]')];
    thumbs.forEach(button=>button.addEventListener('click',()=>{
        if(!main)return;
        main.src=button.dataset.src;
        main.alt=button.dataset.alt||'';
        thumbs.forEach(x=>{
            const active=x===button;
            x.classList.toggle('is-active',active);
            x.setAttribute('aria-current',String(active));
        });
    }));
});

document.querySelectorAll('[data-checkout-form]').forEach(form=>{
    const sameShipping=form.querySelector('[name="UseBillingAsShipping"]');
    const shippingFields=[...form.querySelectorAll('[name^="ShippingAddress."]')];
    const rules={
        Email:{label:'E-mail',required:true,email:true},
        'BillingAddress.FullName':{label:'Jméno',required:true},
        'BillingAddress.Street':{label:'Ulice',required:true},
        'BillingAddress.City':{label:'Město',required:true},
        'BillingAddress.PostalCode':{label:'PSČ',required:true},
        'BillingAddress.CountryCode':{label:'Země',required:true,country:true},
        'BillingAddress.Phone':{label:'Telefon',phone:true},
        'ShippingAddress.FullName':{label:'Jméno dodací adresy',required:()=>!sameShipping?.checked},
        'ShippingAddress.Street':{label:'Ulice dodací adresy',required:()=>!sameShipping?.checked},
        'ShippingAddress.City':{label:'Město dodací adresy',required:()=>!sameShipping?.checked},
        'ShippingAddress.PostalCode':{label:'PSČ dodací adresy',required:()=>!sameShipping?.checked},
        'ShippingAddress.CountryCode':{label:'Země dodací adresy',required:()=>!sameShipping?.checked,country:true}
    };

    const spanFor=input=>form.querySelector(`[data-valmsg-for="${CSS.escape(input.name)}"]`);
    const isRequired=rule=>typeof rule.required==='function'?rule.required():rule.required;
    const setMessage=(input,message)=>{
        const span=spanFor(input);
        if(span){
            span.textContent=message;
            span.classList.toggle('field-validation-error',Boolean(message));
            span.classList.toggle('field-validation-valid',!message);
        }
        input.classList.toggle('is-invalid',Boolean(message));
        input.classList.toggle('is-valid',!message&&Boolean(input.value.trim()));
        input.setAttribute('aria-invalid',String(Boolean(message)));
    };
    const validate=input=>{
        if(input.disabled)return true;
        const rule=rules[input.name];
        if(!rule)return true;
        const value=input.value.trim();
        let message='';
        if(isRequired(rule)&&!value)message=`${rule.label} je povinné pole.`;
        else if(rule.email&&value&&!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value))message='Zadejte e-mail ve správném formátu.';
        else if(rule.country&&value&&!/^[A-Z]{2}$/.test(value))message='Země musí být dvoupísmenný kód, například CZ.';
        else if(rule.phone&&value&&!/^(\+420\s?)?[1-9][0-9]{2}\s?[0-9]{3}\s?[0-9]{3}$/.test(value))message='Zadejte telefon jako 9 číslic, například 774 123 456 nebo +420 774 123 456.';
        setMessage(input,message);
        return !message;
    };
    const syncShipping=()=>{
        const disabled=Boolean(sameShipping?.checked);
        shippingFields.forEach(input=>{
            input.disabled=disabled;
            if(disabled){
                input.classList.remove('is-invalid','is-valid','input-validation-error');
                input.setAttribute('aria-invalid','false');
                const span=spanFor(input);
                if(span)span.textContent='';
            }else{
                validate(input);
            }
        });
    };

    Object.keys(rules).forEach(name=>{
        const input=form.elements[name];
        input?.addEventListener('input',()=>validate(input));
        input?.addEventListener('blur',()=>validate(input));
        if(input?.classList.contains('input-validation-error'))validate(input);
    });
    sameShipping?.addEventListener('change',syncShipping);
    syncShipping();

    form.addEventListener('submit',event=>{
        syncShipping();
        const inputs=Object.keys(rules).map(name=>form.elements[name]).filter(Boolean);
        const valid=inputs.every(validate);
        if(!valid){
            event.preventDefault();
            inputs.find(input=>input.classList.contains('is-invalid'))?.focus();
        }
    });
});
