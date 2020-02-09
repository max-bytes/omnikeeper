
CREATE TYPE attributestate AS ENUM (
    'new',
    'changed',
    'removed',
    'renewed'
);


ALTER TYPE attributestate OWNER TO postgres;

--
-- TOC entry 202 (class 1259 OID 16394)
-- Name: ci; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.ci (
    id bigint NOT NULL,
    identity text NOT NULL
);

ALTER TABLE public.ci
    ADD CONSTRAINT u_identity UNIQUE ("identity");

ALTER TABLE public.ci OWNER TO postgres;

--
-- TOC entry 205 (class 1259 OID 16428)
-- Name: anchor_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.ci ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.anchor_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 203 (class 1259 OID 16402)
-- Name: attribute; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.attribute (
    id bigint NOT NULL,
    name text NOT NULL,
    ci_id bigint NOT NULL,
    type character varying NOT NULL,
    value text NOT NULL,
    activation_time timestamp with time zone NOT NULL,
    layer_id bigint NOT NULL,
    state public.attributestate DEFAULT 'new'::public.attributestate NOT NULL
);


ALTER TABLE public.attribute OWNER TO postgres;

--
-- TOC entry 206 (class 1259 OID 16430)
-- Name: attribute_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.attribute ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.attribute_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 204 (class 1259 OID 16415)
-- Name: layer; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.layer (
    id bigint NOT NULL,
    name character varying NOT NULL
);

ALTER TABLE public.layer
    ADD CONSTRAINT u_name UNIQUE ("name");

ALTER TABLE public.layer OWNER TO postgres;

--
-- TOC entry 207 (class 1259 OID 16432)
-- Name: layer_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.layer ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.layer_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 2707 (class 2606 OID 16401)
-- Name: ci anchor_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.ci
    ADD CONSTRAINT anchor_pkey PRIMARY KEY (id);


--
-- TOC entry 2709 (class 2606 OID 16409)
-- Name: attribute attribute_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.attribute
    ADD CONSTRAINT attribute_pkey PRIMARY KEY (id);


--
-- TOC entry 2711 (class 2606 OID 16422)
-- Name: layer layer_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.layer
    ADD CONSTRAINT layer_pkey PRIMARY KEY (id);


--
-- TOC entry 2712 (class 2606 OID 16410)
-- Name: attribute f_anchor; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.attribute
    ADD CONSTRAINT f_anchor FOREIGN KEY (ci_id) REFERENCES public.ci(id) ON UPDATE RESTRICT ON DELETE RESTRICT NOT VALID;


--
-- TOC entry 2713 (class 2606 OID 16423)
-- Name: attribute f_layer; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.attribute
    ADD CONSTRAINT f_layer FOREIGN KEY (layer_id) REFERENCES public.layer(id) NOT VALID;
