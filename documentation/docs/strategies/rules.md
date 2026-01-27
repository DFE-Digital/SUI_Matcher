# Rule Definitions

Please refer to the following definitions for the individual rules used within the search strategies.

## Exact All

Exact search with all provided values.

`_exact-match`=`true`, <br>
`family`=`harley`, <br>
`given`=`topper`, <br>
`birthdate`=`eq1960-06-09`, <br>
`gender`=`male`<br>

## Exact GFD

Exact search with given name, family name and DOB.

`_exact-match`=`true`, <br>
`family`=`harley`,<br>
`given`=`topper`, <br>
`birthdate`=`eq1960-06-09`<br>

## Fuzzy All

Fuzzy search with all provided values.

`_fuzzy-match`=`true`,<br>
`family`=`harley`, <br>
`given`=`topper`,<br>
`birthdate`=`eq1960-06-09`,<br>
`gender`=`male`,<br>
`address-postalcode`=`WN4 9BP`<br>

## Fuzzy GFD

Fuzzy search with given name, family name, and date of birth.

`_fuzzy-match`=`true`, <br>
`family`=`harley`, <br>
`given`=`topper`, <br>
`birthdate`=`eq1960-06-09`<br>

## Fuzzy GFD (Postcode Wildcard)

Fuzzy search with given name, family name, and date of birth, and postcode as wildcard.

`_fuzzy-match`=`true`, <br>
`family`=`harley`, <br>
`given`=`topper`, <br>
`birthdate`=`eq1960-06-09`,<br>
`address-postalcode`=`WN*`<br>

## Fuzzy GFD Range

Fuzzy search with given name, family name and DOB range 6 months either side of given date.

`_fuzzy-match`=`true`,<br>
`family`=`harley`, <br>
`given`=`topper`, <br>
`birthdate`=`ge1960-01-09`&`birthdate`=`le1961-09-06`<br>

## Fuzzy GFD Range (Postcode)

Fuzzy search with given name, family name, DOB range 6 months either side of given date, and postcode.

`_fuzzy-match`=`true`, <br>
`family`=`harley`, <br>
`given`=`topper`, <br>
`birthdate`=`ge1960-01-09`&`birthdate`=`le1960-07-09`,<br>
`address-postalcode`=`WN4 9BP`<br>

## Fuzzy GFD Range (Postcode Wildcard)

Fuzzy search with given name, family name, DOB range 6 months either side of given date, and postcode as wildcard.

`_fuzzy-match`=`true`, <br>
`family`=`harley`, <br>
`given`=`topper`, <br>
`birthdate`=`ge1960-01-09`&`birthdate`=`le1960-07-09`,<br>
`address-postalcode`=`WN*`<br>

## Fuzzy Alt DOB

Fuzzy search with given name, family name and DOB. Day swapped with month if day equal to or less than 12.

`_fuzzy-match`=`true`,<br>
`family`=`harley`, <br>
`given`=`topper`, <br>
`birthdate`=`eq1960-09-06`<br>

## Non-Fuzzy All

Non-fuzzy search with all provided values.

`_exact-match`=`false`,<br>
`family`=`harley`,<br>
`given`=`topper`, <br>
`birthdate`=`eq1960-06-09`,<br>
`gender`=`male`,<br>
`address-postalcode`=`WN4 9BP`, <br>
`history=true`<br>

## Non-Fuzzy All (Postcode Wildcard)

Non-fuzzy search with all provided values, and postcode as wildcard.

`_exact-match`=`false`,<br>
`family`=`harley`,<br>
`given`=`topper`,<br>
`birthdate`=`eq1960-06-09`,<br>
`address-postalcode`=`WN*`,<br>
`history=true`<br>

## Non-Fuzzy GFD

Non-fuzzy search with given name, family name, and DOB.

`_exact-match`=`false`, <br>
`family`=`harley`, <br>
`given`=`topper`, <br>
`birthdate`=`eq1960-06-09`, <br>
`history=true`<br>

## Non-Fuzzy GFD (Postcode)

Non-fuzzy search with given name, family name, DOB, and postcode.

`_exact-match`=`false`, <br>
`family`=`harley`, <br>
`given`=`topper`, <br>
`birthdate`=`eq1960-06-09`,<br>
`address-postalcode`=`WN4 9BP`,<br>
`history=true`<br>

## Non-Fuzzy GFD Range

Non-fuzzy search with given name, family name, and DOB range 6 months either side of given date.

`_exact-match`=`false`, <br>
`family`=`harley`, <br>
`given`=`topper`, <br>
`birthdate`=`ge1960-01-09`&`birthdate`=`le1960-07-09`,<br>
`history=true`<br>

## Non-Fuzzy GFD Range (Postcode)

Non-fuzzy search with given name, family name, DOB range 6 months either side of given date, and postcode.

`_exact-match`=`false`,<br>
`family`=`harley`,<br>
`given`=`topper`,<br>
`birthdate`=`ge1960-01-09`&`birthdate`=`le1960-07-09`,<br>
`address-postalcode`=`WN4 9BP`,<br>
`history=true`<br>

## Non-Fuzzy GFD Range (Postcode Wildcard)

Non-fuzzy search with given name, family name, DOB range 6 months either side of given date, and postcode as wildcard.

`_exact-match`=`false`,<br>
`family`=`harley`,<br>
`given`=`topper`,<br>
`birthdate`=`ge1960-01-09`&`birthdate`=`le1960-07-09`,<br>
`address-postalcode`=`WN4 9BP`,<br>
`history=true`<br>
