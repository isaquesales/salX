# Calculadora de passo a passo salhttps://youtu.be/-KqcL-tfWIc?si=fcatTEhYbOKjuP7NX
- linguagem matemática com parser e avaliação simbólica

# execução
- `dotnet run --project SalX.Usage`
- `dotnet run --project SalX.Usage -- f`
- modo padrão mostra passos de simplificação
- com `f` mostra só o resultado final

# sintaxe
- números inteiros `10`
- números decimais `10.5`
- frações `1/3`
- constantes `pi` `e`
- operadores `+` `-` `*` `/` `%` `^`
- chamada de função `nome(arg1, arg2)`
- argumentos nomeados `nome(x: 2, y: 3)`
- acesso de método `obj.metodo(...)`
- acesso de propriedade `obj.propriedade`

# funções nativas
- trigonometria `sin(x)` `cos(x)` `tan(x)`
- log e expoente `ln(x)` `log(x)` `exp(x)` `sqrt(x)`
- utilitárias `abs(x)` `floor(x)` `ceil(x)` `rad(x)`
- agregação `max(...)` `min(...)`

# funções definidas pelo usuário
- via API `FunctionRegistry.DefineFunction(nome, parametros, expressão)`
- exemplo `f(a,b)=a^2+b` e depois `f(3,4)`

# sequências
- construtores PA `ap(...)` `pa(...)` `arit(...)`
- construtores PG `geo(...)` `pg(...)` `gp(...)`
- argumentos comuns
- `a1` primeiro termo
- `d` razão da PA
- `r` razão da PG
- termos indexados `a2` `a5` ...
- termo final `an` com `n`

# overloads de construtor
- `ap(a1, d)`
- `ap(a1, an, n)`
- `ap(a1, d, an, n)`
- `geo(a1, r)`
- `geo(a1, an, n)`
- `geo(a1, r, an, n)`
- também aceitam argumentos nomeados

# métodos de sequência
- `first` ou `a1`
- `ratio` ou `r`
- `difference` ou `d`
- `term(n)` ou `an(n)`
- `sum(n)` ou `sn(n)`
- `indexOf(an)`
- `solve(...)`
- `range(start, end)` novo

# range
- funciona em PA e PG
- recebe dois índices inteiros `>= 1`
- aceita posicional `range(3, 8)`
- aceita nomeado `range(start: 3, end: 8)` e aliases `from/to` `i/j`
- retorna os termos no trecho informado

# exemplos rápidos
- `ap(2, 3).term(5)` -> `a5`
- `ap(a1: 2, d: 3).sum(5)` -> `S5`
- `ap(2, 3).range(2, 6)` -> mostra `a2` até `a6`
- `geo(a1: 3, r: 2).term(4)` -> `a4`
- `gp(3, 2).range(from: 1, to: 5)` -> mostra `a1` até `a5`
- `pg(a1: 5, a4: 40).solve(r: 0)` resolve parâmetros pendentes

# observações
- validações rejeitam `NaN` e `Infinity`
- índices de termo devem ser inteiros `>= 1`
- se dados da sequência forem inconsistentes o parser retorna erro
