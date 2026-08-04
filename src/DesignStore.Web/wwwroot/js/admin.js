document.querySelectorAll('[data-order-contact]').forEach(root => {
    const modal = root.querySelector('[data-order-contact-modal]');
    const form = root.querySelector('[data-order-contact-form]');
    const error = root.querySelector('[data-order-contact-error]');
    const open = () => modal?.showModal ? modal.showModal() : modal?.setAttribute('open', '');
    const close = () => modal?.close ? modal.close() : modal?.removeAttribute('open');

    root.querySelector('[data-order-contact-open]')?.addEventListener('click', open);
    root.querySelectorAll('[data-order-contact-close]').forEach(button => button.addEventListener('click', close));
    modal?.addEventListener('click', event => {
        if (event.target === modal) close();
    });

    form?.addEventListener('submit', async event => {
        event.preventDefault();
        clearOrderContactErrors(form, error);

        const submit = form.querySelector('[type="submit"]');
        submit?.setAttribute('disabled', 'disabled');
        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }
            });
            const payload = await response.json().catch(() => ({}));
            if (!response.ok || payload.ok === false) {
                showOrderContactErrors(form, payload.errors, error, payload.error);
                return;
            }

            updateOrderContact(root, payload);
            close();
        } catch (e) {
            if (error) {
                error.textContent = e.message || 'Údaje se nepodařilo uložit.';
                error.hidden = false;
            }
        } finally {
            submit?.removeAttribute('disabled');
        }
    });

    form?.querySelectorAll('input, select, textarea').forEach(input => {
        input.addEventListener('input', () => clearSingleOrderContactError(input, error));
        input.addEventListener('change', () => clearSingleOrderContactError(input, error));
    });
});

function clearOrderContactErrors(form, error) {
    if (error) {
        error.hidden = true;
        error.textContent = '';
    }

    form.querySelectorAll('.field-error').forEach(element => element.remove());
    form.querySelectorAll('.is-invalid').forEach(element => element.classList.remove('is-invalid'));
}

function clearSingleOrderContactError(input, error) {
    input.classList.remove('is-invalid');
    input.closest('label')?.querySelector('.field-error')?.remove();

    if (error && !input.form?.querySelector('.field-error')) {
        error.hidden = true;
        error.textContent = '';
    }
}

function showOrderContactErrors(form, errors, error, fallback) {
    const list = Array.isArray(errors) ? errors : [];

    if (!list.length) {
        if (error) {
            error.textContent = fallback || 'Údaje se nepodařilo uložit.';
            error.hidden = false;
        }
        return;
    }

    let firstInput = null;
    for (const item of list) {
        const field = item.field || item.Field || '';
        const message = item.message || item.Message || fallback || 'Zkontrolujte pole.';
        const input = field ? form.querySelector(`[name="${cssEscape(field)}"]`) : null;

        if (!input) continue;

        input.classList.add('is-invalid');
        firstInput ??= input;

        const label = input.closest('label');
        if (label && !label.querySelector('.field-error')) {
            const note = document.createElement('span');
            note.className = 'field-error';
            note.textContent = message;
            label.appendChild(note);
        }
    }

    if (error) {
        error.textContent = fallback || 'Zkontrolujte zvýrazněná pole.';
        error.hidden = false;
    }

    firstInput?.focus({ preventScroll: false });
}

function cssEscape(value) {
    if (window.CSS?.escape) return CSS.escape(value);
    return value.replace(/["\\]/g, '\\$&');
}

function updateOrderContact(root, payload) {
    const billing = payload.billing || payload.billingAddress || {};
    const shipping = payload.shipping || payload.shippingAddress || {};
    const setText = (selector, value) => {
        const element = root.querySelector(selector);
        if (element) element.textContent = value || '';
    };
    const updateAddress = (kind, address) => {
        const summary = root.querySelector(`[data-address-summary="${kind}"]`);
        if (!summary) return;
        summary.querySelector('[data-field="fullName"]').textContent = address.fullName || address.FullName || '';
        summary.querySelector('[data-field="company"]').textContent = address.company || address.Company || '';
        summary.querySelector('[data-field="street"]').textContent = address.street || address.Street || '';
        summary.querySelector('[data-field="postalCode"]').textContent = address.postalCode || address.PostalCode || '';
        summary.querySelector('[data-field="city"]').textContent = address.city || address.City || '';
        summary.querySelector('[data-field="countryCode"]').textContent = address.countryCode || address.CountryCode || '';
        setText(`[data-address-phone="${kind}"]`, address.phone || address.Phone || '');
    };

    setText('[data-contact-email]', payload.email || payload.Email || '');
    updateAddress('billing', billing);
    updateAddress('shipping', shipping);
}

document.querySelector('[data-add-variant]')?.addEventListener('click', () => {
    const root = document.querySelector('#variants');
    const i = root.children.length;
    root.insertAdjacentHTML('beforeend', `<div class="nested-row"><label>SKU<input name="Variants[${i}].Sku"></label><label>Název CZ<input name="Variants[${i}].Name"></label><label>Název EN<input name="Variants[${i}].NameEn"></label><label>Název DE<input name="Variants[${i}].NameDe"></label><label>Cena<input name="Variants[${i}].Price" type="number" step="0.01"></label><label>Sklad<input name="Variants[${i}].StockQuantity" type="number" min="0" value="0"></label><label>Dostupnost<select name="Variants[${i}].AvailabilityStatus"><option value="1">InStock</option><option value="2">MadeToOrder</option><option value="3">Unavailable</option><option value="4">Discontinued</option></select></label></div>`);
});

document.querySelector('[data-add-option]')?.addEventListener('click', () => {
    const root = document.querySelector('#options');
    const i = root.children.length;
    root.insertAdjacentHTML('beforeend', `<div class="nested-row"><label>Název<input name="Options[${i}].Name"></label><label>Typ<select name="Options[${i}].Type"><option value="1">Výběr</option><option value="2">Text</option><option value="3">Gravírování</option></select></label><label><input name="Options[${i}].IsRequired" type="checkbox" value="true"> Povinná</label><label>Hodnota<input name="Options[${i}].Values[0].Name"></label><label>Příplatek<input name="Options[${i}].Values[0].PriceAdjustment" type="number" step="0.01" value="0"></label></div>`);
});

document.querySelector('[data-translate-product]')?.addEventListener('click', () => {
    const source = readTranslation('cs-CZ');
    const note = document.querySelector('[data-translation-note]');
    if (!source.Name && !source.ShortDescription && !source.LongDescription) {
        showTranslationNote(note, 'Nejdřív vyplňte český název nebo popis.');
        return;
    }

    writeTranslation('en', createDraft(source, 'en'));
    writeTranslation('de', createDraft(source, 'de'));
    showTranslationNote(note, 'Návrhy překladů jsou vyplněné. Berte je jako pracovní text a před uložením je zkontrolujte.');
});

document.querySelector('[data-translate-category]')?.addEventListener('click', () => {
    const source = readTranslation('cs-CZ');
    const note = document.querySelector('[data-translation-note]');
    if (!source.Name && !source.ShortDescription && !source.LongDescription) {
        showTranslationNote(note, 'Nejdřív vyplňte český název nebo popis kategorie.');
        return;
    }

    writeTranslation('en', createCategoryDraft(source, 'en'));
    writeTranslation('de', createCategoryDraft(source, 'de'));
    showTranslationNote(note, 'Návrhy překladů kategorie jsou vyplněné. Před uložením je prosím ještě lidsky zkontrolujte.');
});

document.querySelectorAll('[data-translation-tab]').forEach(button => {
    button.addEventListener('click', () => activateTranslationTab(button.dataset.translationTab));
});

const modeInputs = document.querySelectorAll('[data-product-mode]');
if (modeInputs.length) {
    modeInputs.forEach(input => input.addEventListener('change', applyProductMode));
    applyProductMode();
}

const categoryTemplateSelect = document.querySelector('[data-category-template-select]');
categoryTemplateSelect?.addEventListener('change', () => {
    const option = categoryTemplateSelect.selectedOptions[0];
    const key = normalizeTemplateKey(option?.dataset.categorySlug || option?.dataset.categoryName || '');
    const template = categoryTemplates[key];
    if (!template) return;
    for (const culture of Object.keys(template)) fillTranslationBlanks(culture, template[culture]);
    const note = document.querySelector('[data-translation-note]');
    showTranslationNote(note, `Doplnil jsem prázdná textová pole podle kategorie „${option.dataset.categoryName}“. Ručně vyplněná pole jsem nepřepsal.`);
});

function applyProductMode() {
    const editor = document.querySelector('[data-product-editor]');
    const selected = editor?.querySelector('[data-product-mode]:checked')?.value || 'ready';
    const isCustom = selected === 'custom';

    editor?.classList.toggle('is-custom-mode', isCustom);
    editor?.classList.toggle('is-ready-mode', !isCustom);
    editor?.querySelectorAll('[data-ready-field]').forEach(x => x.hidden = isCustom);
    editor?.querySelectorAll('[data-custom-field]').forEach(x => x.hidden = !isCustom);

    const availability = editor?.querySelector('[data-availability-status]');
    const stock = editor?.querySelector('[data-stock-quantity]');
    const trackInventory = editor?.querySelector('[data-track-inventory]');
    const canBeMade = editor?.querySelector('[name="CanBeMadeToOrder"]');
    const startingPrice = editor?.querySelector('[name="StartingPrice"]');
    const basePrice = editor?.querySelector('[name="BasePrice"]');

    if (availability) availability.value = isCustom ? availability.dataset.availabilityMadeToOrder : availability.dataset.availabilityInStock;
    if (stock) stock.value = isCustom ? '0' : (stock.value || '1');
    if (trackInventory) trackInventory.checked = !isCustom;
    if (canBeMade) canBeMade.checked = isCustom;
    if (startingPrice && basePrice && isCustom && !startingPrice.value && basePrice.value) startingPrice.value = basePrice.value;
}

function activateTranslationTab(culture) {
    document.querySelectorAll('[data-translation-tab]').forEach(button => {
        const isActive = button.dataset.translationTab === culture;
        button.classList.toggle('is-active', isActive);
        button.setAttribute('aria-selected', isActive ? 'true' : 'false');
    });

    document.querySelectorAll('[data-translation-culture]').forEach(panel => {
        const isActive = panel.dataset.translationCulture === culture;
        panel.hidden = !isActive;
        panel.classList.toggle('is-active', isActive);
    });
}

function fillTranslationBlanks(culture, values) {
    const fieldset = document.querySelector(`[data-translation-culture="${culture}"]`);
    fieldset?.querySelectorAll('[data-translation-field]').forEach(input => {
        const value = values[input.dataset.translationField];
        if (value !== undefined && !input.value.trim()) input.value = value;
    });
}

function readTranslation(culture) {
    const fieldset = document.querySelector(`[data-translation-culture="${culture}"]`);
    const result = {};
    fieldset?.querySelectorAll('[data-translation-field]').forEach(input => {
        result[input.dataset.translationField] = input.value.trim();
    });
    return result;
}

function writeTranslation(culture, values) {
    const fieldset = document.querySelector(`[data-translation-culture="${culture}"]`);
    fieldset?.querySelectorAll('[data-translation-field]').forEach(input => {
        const value = values[input.dataset.translationField];
        if (value !== undefined) input.value = value;
    });
}

function createDraft(source, culture) {
    const name = translateText(source.Name, culture);
    const shortDescription = translateText(source.ShortDescription, culture);
    const longDescription = translateText(source.LongDescription, culture);
    const story = translateText(source.Story, culture);
    const materials = translateText(source.Materials, culture);
    const care = translateText(source.CareInstructions, culture);
    const shipping = translateText(source.ShippingInformation, culture);
    const seoTitle = translateText(source.SeoTitle || source.Name, culture);
    const seoDescription = translateText(source.SeoDescription || source.ShortDescription, culture);

    return {
        Name: name,
        Slug: slugify(name || source.Name),
        ShortDescription: shortDescription,
        LongDescription: longDescription,
        Story: story,
        Materials: materials,
        CareInstructions: care,
        ShippingInformation: shipping,
        SeoTitle: seoTitle.substring(0, 70),
        SeoDescription: seoDescription.substring(0, 170)
    };
}

function createCategoryDraft(source, culture) {
    const name = translateText(source.Name, culture);
    const shortDescription = translateText(source.ShortDescription, culture);
    const longDescription = translateText(source.LongDescription, culture);
    const seoTitle = translateText(source.SeoTitle || source.Name, culture);
    const seoDescription = translateText(source.SeoDescription || source.ShortDescription || source.LongDescription, culture);

    return {
        Name: name,
        Slug: slugify(name || source.Name),
        ShortDescription: shortDescription,
        LongDescription: longDescription,
        SeoTitle: seoTitle.substring(0, 70),
        SeoDescription: seoDescription.substring(0, 170)
    };
}

function translateText(text, culture) {
    if (!text) return '';
    const dictionaries = {
        en: [
            ['Autorský', 'Original'], ['autorský', 'original'], ['objekt', 'object'], ['Objekt', 'Object'],
            ['produkt', 'product'], ['Produkt', 'Product'], ['nábytek', 'furniture'], ['světlo', 'light'],
            ['kůže', 'leather'], ['kožený', 'leather'], ['dřevo', 'wood'], ['železo', 'iron'], ['kámen', 'stone'],
            ['materiál', 'material'], ['materiály', 'materials'], ['ruční', 'handmade'], ['práce', 'work'],
            ['poctivé', 'honest'], ['poctivý', 'honest'], ['přírodní', 'natural'], ['kolekce', 'collection'],
            ['vyrobený', 'made'], ['vyrobená', 'made'], ['vytvořený', 'crafted'], ['vytvořená', 'crafted'],
            ['malé sérii', 'small series'], ['s důrazem na', 'with attention to'], ['detail', 'detail'],
            ['péče', 'care'], ['doprava', 'shipping'], ['rozměry', 'dimensions']
        ],
        de: [
            ['Autorský', 'Eigenständiges'], ['autorský', 'eigenständiges'], ['objekt', 'Objekt'], ['Objekt', 'Objekt'],
            ['produkt', 'Produkt'], ['Produkt', 'Produkt'], ['nábytek', 'Möbel'], ['světlo', 'Leuchte'],
            ['kůže', 'Leder'], ['kožený', 'Leder'], ['dřevo', 'Holz'], ['železo', 'Eisen'], ['kámen', 'Stein'],
            ['materiál', 'Material'], ['materiály', 'Materialien'], ['ruční', 'handwerkliche'], ['práce', 'Arbeit'],
            ['poctivé', 'ehrliche'], ['poctivý', 'ehrlicher'], ['přírodní', 'natürliche'], ['kolekce', 'Kollektion'],
            ['vyrobený', 'gefertigt'], ['vyrobená', 'gefertigt'], ['vytvořený', 'gestaltet'], ['vytvořená', 'gestaltet'],
            ['malé sérii', 'Kleinserie'], ['s důrazem na', 'mit Fokus auf'], ['detail', 'Detail'],
            ['péče', 'Pflege'], ['doprava', 'Versand'], ['rozměry', 'Abmessungen']
        ]
    };
    let value = text;
    for (const [from, to] of dictionaries[culture] || []) value = value.replaceAll(from, to);
    return value;
}

function slugify(value) {
    return value.toLowerCase()
        .normalize('NFD').replace(/[\u0300-\u036f]/g, '')
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '')
        .substring(0, 220);
}

function showTranslationNote(note, message) {
    if (!note) return;
    note.textContent = message;
    note.hidden = false;
}

function normalizeTemplateKey(value) {
    return slugify(value).replaceAll('-', '');
}

const categoryTemplates = {
    kozenekabelky: {
        'cs-CZ': {
            ShortDescription: 'Kožená kabelka z poctivé usně, navržená pro každodenní nošení i tiché večerní chvíle.',
            LongDescription: 'Každý kus vzniká v malé sérii s důrazem na pevnost švů, přirozenou kresbu kůže a nadčasový tvar. Kabelka je stavěná tak, aby dobře držela tvar, krásně stárla a postupně získávala vlastní patinu.',
            Story: 'Vzniká pomalu, s respektem k materiálu a k ruce, která ji bude nosit. Každá kabelka má drobné rozdíly v kresbě kůže, díky kterým je skutečně jedinečná.',
            Materials: 'Pravá useň, pevné kování a pečlivě zvolené detaily. Konkrétní odstín a struktura se mohou přirozeně lišit podle partie kůže.',
            CareInstructions: 'Kůži čistěte jemně navlhčeným hadříkem. Nevystavujte ji dlouhodobě přímému slunci, promáčení ani agresivním čisticím prostředkům. Podle potřeby použijte vhodný balzám na kůži.',
            ShippingInformation: 'Hotový kus odesíláme po kontrole a bezpečném zabalení. Zakázkovou výrobu potvrzujeme individuálně podle dostupnosti materiálu.',
            SeoTitle: 'Kožená kabelka Wild Spirit',
            SeoDescription: 'Autorská kožená kabelka z poctivé usně, vyrobená v malé sérii s důrazem na detail, materiál a nadčasový charakter.'
        },
        en: {
            ShortDescription: 'A leather handbag made from honest hide, designed for everyday use and quiet evening moments.',
            LongDescription: 'Each piece is made in a small series with attention to strong stitching, the natural grain of leather and a timeless silhouette. The bag is made to hold its shape, age beautifully and develop its own patina over time.',
            Story: 'Made slowly, with respect for the material and for the hand that will carry it. Every bag carries subtle differences in the leather grain, making each piece genuinely individual.',
            Materials: 'Genuine leather, durable hardware and carefully selected details. The exact shade and structure may naturally vary between leather hides.',
            CareInstructions: 'Clean the leather with a slightly damp soft cloth. Avoid long exposure to direct sun, soaking and aggressive cleaning products. Use a suitable leather balm when needed.',
            ShippingInformation: 'Ready-made pieces are shipped after inspection and careful packaging. Made-to-order pieces are confirmed individually according to material availability.',
            SeoTitle: 'Wild Spirit leather handbag',
            SeoDescription: 'An original leather handbag made in a small series with attention to detail, material and timeless character.'
        },
        de: {
            ShortDescription: 'Eine Ledertasche aus ehrlichem Leder, entworfen für den Alltag und stille Abendmomente.',
            LongDescription: 'Jedes Stück entsteht in Kleinserie mit Fokus auf feste Nähte, natürliche Lederstruktur und eine zeitlose Form. Die Tasche ist so gefertigt, dass sie ihre Form hält, schön altert und mit der Zeit eine eigene Patina entwickelt.',
            Story: 'Langsam gefertigt, mit Respekt vor dem Material und vor der Hand, die sie tragen wird. Jede Tasche zeigt feine Unterschiede in der Lederstruktur und bleibt dadurch ein echtes Einzelstück.',
            Materials: 'Echtes Leder, robuste Beschläge und sorgfältig gewählte Details. Farbton und Struktur können je nach Lederpartie natürlich variieren.',
            CareInstructions: 'Reinigen Sie das Leder mit einem leicht feuchten, weichen Tuch. Vermeiden Sie lange direkte Sonne, Durchnässung und aggressive Reinigungsmittel. Bei Bedarf einen geeigneten Lederbalsam verwenden.',
            ShippingInformation: 'Fertige Stücke versenden wir nach Kontrolle und sorgfältiger Verpackung. Maßanfertigungen bestätigen wir individuell je nach Materialverfügbarkeit.',
            SeoTitle: 'Wild Spirit Ledertasche',
            SeoDescription: 'Eine eigenständige Ledertasche aus Kleinserie, gefertigt mit Fokus auf Detail, Material und zeitlosen Charakter.'
        }
    },
    kozenepasky: {
        'cs-CZ': {
            ShortDescription: 'Kožený pásek z pevné usně, který má vydržet roky každodenního nošení.',
            LongDescription: 'Pásek vzniká z vybrané kůže a pevného kování. Je navržený jednoduše, bez zbytečného efektu, aby dobře fungoval dnes a získával charakter časem.',
            Story: 'Tichý doplněk, který se nevnucuje. Právě používáním získává vlastní stopu, měkne a přirozeně stárne.',
            Materials: 'Pravá useň, kovová přezka a pevné zpracování hran.',
            CareInstructions: 'Chraňte před promáčením a dlouhým přímým sluncem. Čistěte jemným hadříkem a občas ošetřete balzámem na kůži.',
            ShippingInformation: 'Hotový kus odesíláme po kontrole velikosti a finální úpravě.',
            SeoTitle: 'Kožený pásek Wild Spirit',
            SeoDescription: 'Poctivý kožený pásek z pevné usně, vyrobený s důrazem na detail a dlouhou životnost.'
        },
        en: {
            ShortDescription: 'A leather belt made from durable hide, built for years of everyday wear.',
            LongDescription: 'The belt is made from selected leather and solid hardware. It is intentionally simple, made to work today and gain character with time.',
            Story: 'A quiet accessory that does not need to shout. Through use it gains its own marks, softens and ages naturally.',
            Materials: 'Genuine leather, metal buckle and carefully finished edges.',
            CareInstructions: 'Protect from soaking and long direct sunlight. Clean with a soft cloth and occasionally treat with leather balm.',
            ShippingInformation: 'Ready-made pieces are shipped after size check and final finishing.',
            SeoTitle: 'Wild Spirit leather belt',
            SeoDescription: 'An honest leather belt made from durable hide with attention to detail and long life.'
        },
        de: {
            ShortDescription: 'Ein Ledergürtel aus robustem Leder, gemacht für viele Jahre Alltag.',
            LongDescription: 'Der Gürtel entsteht aus ausgewähltem Leder und soliden Beschlägen. Er ist bewusst schlicht gestaltet, funktioniert heute und gewinnt mit der Zeit Charakter.',
            Story: 'Ein stilles Accessoire, das sich nicht aufdrängt. Durch Gebrauch bekommt es eigene Spuren, wird weicher und altert natürlich.',
            Materials: 'Echtes Leder, Metallschnalle und sorgfältig verarbeitete Kanten.',
            CareInstructions: 'Vor Durchnässung und langer direkter Sonne schützen. Mit einem weichen Tuch reinigen und gelegentlich mit Lederbalsam pflegen.',
            ShippingInformation: 'Fertige Stücke versenden wir nach Größenkontrolle und finaler Verarbeitung.',
            SeoTitle: 'Wild Spirit Ledergürtel',
            SeoDescription: 'Ein ehrlicher Ledergürtel aus robustem Leder, gefertigt mit Fokus auf Detail und lange Lebensdauer.'
        }
    },
    kozenedoplnky: {
        'cs-CZ': {
            ShortDescription: 'Kožený doplněk z poctivého materiálu pro každodenní používání.',
            LongDescription: 'Jednoduchý, funkční a materiálově výrazný doplněk, který je navržený tak, aby obstál v běžném používání a časem získal osobní charakter.',
            Story: 'Malý předmět s vlastním rytmem. Důležitý je dotek materiálu, hrana, steh a klidná funkce.',
            Materials: 'Pravá useň, pevné šití a detaily zvolené podle konkrétního použití.',
            CareInstructions: 'Čistěte jemným hadříkem a podle potřeby ošetřete vhodným přípravkem na kůži.',
            ShippingInformation: 'Hotové kusy odesíláme po kontrole a bezpečném zabalení.',
            SeoTitle: 'Kožený doplněk Wild Spirit',
            SeoDescription: 'Autorský kožený doplněk z poctivého materiálu, navržený pro každodenní používání.'
        },
        en: {
            ShortDescription: 'A leather accessory made from honest material for everyday use.',
            LongDescription: 'A simple, functional and material-focused accessory designed to withstand everyday use and gain personal character with time.',
            Story: 'A small object with its own rhythm. The touch of material, the edge, the stitch and quiet function matter here.',
            Materials: 'Genuine leather, strong stitching and details selected for the specific use.',
            CareInstructions: 'Clean with a soft cloth and treat with a suitable leather care product when needed.',
            ShippingInformation: 'Ready-made pieces are shipped after inspection and careful packaging.',
            SeoTitle: 'Wild Spirit leather accessory',
            SeoDescription: 'An original leather accessory made from honest material, designed for everyday use.'
        },
        de: {
            ShortDescription: 'Ein Lederaccessoire aus ehrlichem Material für den täglichen Gebrauch.',
            LongDescription: 'Ein schlichtes, funktionales und materialstarkes Accessoire, gemacht für den Alltag und dafür, mit der Zeit persönlichen Charakter zu entwickeln.',
            Story: 'Ein kleiner Gegenstand mit eigenem Rhythmus. Wichtig sind Berührung, Kante, Naht und stille Funktion.',
            Materials: 'Echtes Leder, feste Nähte und Details passend zur konkreten Nutzung.',
            CareInstructions: 'Mit einem weichen Tuch reinigen und bei Bedarf mit geeigneter Lederpflege behandeln.',
            ShippingInformation: 'Fertige Stücke versenden wir nach Kontrolle und sorgfältiger Verpackung.',
            SeoTitle: 'Wild Spirit Lederaccessoire',
            SeoDescription: 'Ein eigenständiges Lederaccessoire aus ehrlichem Material, gestaltet für den täglichen Gebrauch.'
        }
    }
};

categoryTemplates.kozene = categoryTemplates.kozenedoplnky;
categoryTemplates.leatherbags = categoryTemplates.kozenekabelky;
categoryTemplates.leatherbelts = categoryTemplates.kozenepasky;
categoryTemplates.leatheraccessories = categoryTemplates.kozenedoplnky;

document.querySelectorAll('[data-dropzone-form]').forEach(form => {
    const dropzone = form.querySelector('[data-dropzone]');
    const input = form.querySelector('[data-dropzone-input]');
    const list = form.querySelector('[data-dropzone-list]');
    if (!dropzone || !input || !list) return;
    let previewUrls = [];

    const releasePreviews = () => {
        previewUrls.forEach(url => URL.revokeObjectURL(url));
        previewUrls = [];
    };

    const applyFiles = files => {
        const selected = Array.from(files || []).filter(file => file.type.startsWith('image/')).slice(0, 5);
        const transfer = new DataTransfer();
        selected.forEach(file => transfer.items.add(file));
        input.files = transfer.files;
        renderPreviewFiles();
    };

    const renderFiles = () => {
        const files = Array.from(input.files || []);
        if (!files.length) {
            list.textContent = '';
            return;
        }
        list.innerHTML = files.map(file => `<span>${escapeHtml(file.name)} · ${formatBytes(file.size)}</span>`).join('');
    };

    const renderPreviewFiles = () => {
        releasePreviews();
        const files = Array.from(input.files || []);
        if (!files.length) {
            list.textContent = '';
            return;
        }
        list.innerHTML = files.map((file, index) => {
            const previewUrl = URL.createObjectURL(file);
            previewUrls.push(previewUrl);
            return `<article class="dropzone-preview">
                <img src="${previewUrl}" alt="Náhled obrázku ${index + 1}" loading="lazy">
                <span class="dropzone-preview-name">${escapeHtml(file.name)}</span>
                <span class="dropzone-preview-meta">${formatBytes(file.size)}</span>
            </article>`;
        }).join('');
    };

    input.addEventListener('change', () => applyFiles(input.files));
    dropzone.addEventListener('dragover', event => {
        event.preventDefault();
        dropzone.classList.add('is-dragover');
    });
    dropzone.addEventListener('dragleave', () => dropzone.classList.remove('is-dragover'));
    dropzone.addEventListener('drop', event => {
        event.preventDefault();
        dropzone.classList.remove('is-dragover');
        applyFiles(event.dataTransfer?.files);
    });
});

function formatBytes(bytes) {
    if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} kB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

function escapeHtml(value) {
    return value.replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));
}
